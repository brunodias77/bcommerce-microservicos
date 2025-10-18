using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using UserService.Application.Commands.Users.Create;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de autenticação da aplicação
/// </summary>
[ApiController]
[Route("/api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IMediator _mediator;

    public AuthController(ILogger<AuthController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
    
    /// <summary>
    /// Endpoint para registrar um novo usuário no sistema
    /// </summary>
    /// <param name="command">Dados do usuário a ser criado</param>
    /// <returns>ID do usuário criado em caso de sucesso</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados de entrada inválidos</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
    {
        try
        {
            _logger.LogInformation("Iniciando processo de registro de usuário para email: {Email}", command.Email);

            var result = await _mediator.SendAsync<ApiResponse<Guid>>(command);

            if (result.Success)
            {
                _logger.LogInformation("Usuário registrado com sucesso. ID: {UserId}", result.Data);
                return CreatedAtAction(nameof(Register), new { id = result.Data }, result.Data);
            }

            _logger.LogWarning("Falha no registro do usuário: {Message}", result.Message);
            return BadRequest(new
            {
                Status = "Error",
                Message = result.Message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao registrar usuário para email: {Email}", command?.Email);
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Erro interno do servidor ao processar o registro",
                Timestamp = DateTime.UtcNow
            });
        }
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