using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de health check da aplicação
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly UserServiceDbContext _dbContext;
    private readonly HealthCheckService _healthCheckService;

    public HealthController(
        ILogger<HealthController> logger,
        UserServiceDbContext dbContext,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Endpoint de verificação geral da saúde da aplicação
    /// </summary>
    /// <returns>Status geral da aplicação</returns>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            _logger.LogInformation("Executando verificação geral de saúde da aplicação");

            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Application = new
                {
                    Name = "UserService.Api",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                },
                Checks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration.TotalMilliseconds
                    }
                ),
                TotalDuration = healthReport.TotalDuration.TotalMilliseconds
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar verificação geral de saúde");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = "Falha na verificação de saúde da aplicação"
            });
        }
    }

    /// <summary>
    /// Endpoint de verificação específica do banco de dados
    /// </summary>
    /// <returns>Status da conectividade com o banco de dados</returns>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            _logger.LogInformation("Executando verificação de saúde do banco de dados");

            var startTime = DateTime.UtcNow;
            
            // Testa a conectividade com o banco
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            var duration = DateTime.UtcNow - startTime;

            if (canConnect)
            {
                // Executa uma query simples para verificar se o banco está respondendo
                var userCount = await _dbContext.Users.CountAsync();
                
                var response = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Database = new
                    {
                        ConnectionStatus = "Connected",
                        Provider = _dbContext.Database.ProviderName,
                        UserCount = userCount,
                        ResponseTime = duration.TotalMilliseconds
                    }
                };

                _logger.LogInformation("Verificação de saúde do banco de dados bem-sucedida. Usuários: {UserCount}, Tempo: {Duration}ms", 
                    userCount, duration.TotalMilliseconds);

                return Ok(response);
            }
            else
            {
                var response = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Database = new
                    {
                        ConnectionStatus = "Disconnected",
                        Provider = _dbContext.Database.ProviderName,
                        ResponseTime = duration.TotalMilliseconds
                    },
                    Error = "Não foi possível conectar ao banco de dados"
                };

                _logger.LogWarning("Falha na conectividade com o banco de dados. Tempo: {Duration}ms", duration.TotalMilliseconds);

                return StatusCode(503, response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde do banco de dados");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = new
                {
                    ConnectionStatus = "Error",
                    Provider = _dbContext.Database.ProviderName
                },
                Error = "Erro interno ao verificar banco de dados"
            });
        }
    }

    /// <summary>
    /// Endpoint de verificação se a aplicação está pronta para receber requisições
    /// </summary>
    /// <returns>Status de prontidão da aplicação</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            _logger.LogInformation("Executando verificação de prontidão da aplicação");

            var checks = new List<object>();
            var isReady = true;

            // Verifica conectividade com o banco
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                checks.Add(new
                {
                    Name = "Database",
                    Status = canConnect ? "Ready" : "NotReady",
                    Description = canConnect ? "Banco de dados acessível" : "Banco de dados inacessível"
                });
                
                if (!canConnect) isReady = false;
            }
            catch (Exception ex)
            {
                checks.Add(new
                {
                    Name = "Database",
                    Status = "NotReady",
                    Description = $"Erro ao conectar: {ex.Message}"
                });
                isReady = false;
            }

            // Verifica se as configurações essenciais estão presentes
            var connectionString = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            
            checks.Add(new
            {
                Name = "Configuration",
                Status = !string.IsNullOrEmpty(connectionString) ? "Ready" : "NotReady",
                Description = !string.IsNullOrEmpty(connectionString) 
                    ? "Configurações carregadas" 
                    : "Connection string não configurada"
            });

            if (string.IsNullOrEmpty(connectionString)) isReady = false;

            var response = new
            {
                Status = isReady ? "Ready" : "NotReady",
                Timestamp = DateTime.UtcNow,
                Application = new
                {
                    Name = "UserService.Api",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
                },
                Checks = checks
            };

            var statusCode = isReady ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar prontidão da aplicação");
            return StatusCode(503, new
            {
                Status = "NotReady",
                Timestamp = DateTime.UtcNow,
                Error = "Falha na verificação de prontidão"
            });
        }
    }

    /// <summary>
    /// Endpoint de verificação se a aplicação está viva (liveness probe)
    /// </summary>
    /// <returns>Status de vida da aplicação</returns>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        try
        {
            _logger.LogInformation("Executando verificação de vida da aplicação");

            var response = new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow,
                Application = new
                {
                    Name = "UserService.Api",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    Uptime = Environment.TickCount64 / 1000.0 // segundos desde o início
                },
                Server = new
                {
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = GC.GetTotalMemory(false) / 1024 / 1024 // MB
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar vida da aplicação");
            return StatusCode(503, new
            {
                Status = "Dead",
                Timestamp = DateTime.UtcNow,
                Error = "Falha na verificação de vida"
            });
        }
    }
}