using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de autenticação da aplicação
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de teste para verificar se o controller de autenticação está funcionando
    /// </summary>
    /// <returns>Informações básicas do controller de autenticação</returns>
    [HttpGet("test")]
    public IActionResult GetTest()
    {
        try
        {
            _logger.LogInformation("Executando teste do AuthController");

            var response = new
            {
                Status = "Success",
                Message = "AuthController está funcionando corretamente",
                Timestamp = DateTime.UtcNow,
                Controller = new
                {
                    Name = "AuthController",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                }
            };

            _logger.LogInformation("Teste do AuthController executado com sucesso");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar teste do AuthController");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Erro interno no AuthController",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}