using Microsoft.AspNetCore.Mvc;
using UserService.Api.DTOs.Keycloak;
using UserService.Application.Contracts.Keycloak;
using UserService.Infrastructure.Dtos.Keycloak;
using BuildingBlocks.Results;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de integração com Keycloak
/// </summary>
[ApiController]
[Route("/api/keycloak")]
[Produces("application/json")]
public class KeycloakController : ControllerBase
{
    private readonly ILogger<KeycloakController> _logger;
    private readonly IKeycloakService _keycloakService;

    public KeycloakController(
        ILogger<KeycloakController> logger,
        IKeycloakService keycloakService)
    {
        _logger = logger;
        _keycloakService = keycloakService;
    }

    /// <summary>
    /// Endpoint para criar um novo usuário no Keycloak
    /// </summary>
    /// <param name="request">Dados do usuário a ser criado no Keycloak</param>
    /// <returns>Dados do usuário criado em caso de sucesso</returns>
    /// <response code="201">Usuário criado com sucesso no Keycloak</response>
    /// <response code="400">Dados de entrada inválidos</response>
    /// <response code="409">Usuário já existe no Keycloak</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("users")]
    [ProducesResponseType(typeof(CreateKeycloakUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser([FromBody] CreateKeycloakUserRequest request)
    {
        try
        {
            _logger.LogInformation("Iniciando criação de usuário no Keycloak para email: {Email}", request.Email);

            // Validar se o usuário já existe
            var existingUser = await _keycloakService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de criar usuário já existente no Keycloak: {Email}", request.Email);
                return Conflict(ApiResponse.Fail("USER_ALREADY_EXISTS", 
                    $"Já existe um usuário no Keycloak com o email {request.Email}"));
            }

            // Criar usuário no Keycloak
            var createUserKeycloak = new CreateUserKeycloak(
                Username: request.Email,
                Email: request.Email,
                FirstName: request.FirstName,
                LastName: request.LastName,
                Password: request.Password,
                Enabled: request.Enabled,
                EmailVerified: request.EmailVerified,
                Roles: request.Roles ?? new List<string> { "user" }
            );

            var userId = await _keycloakService.CreateUserAsync(createUserKeycloak);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Keycloak não retornou ID do usuário para email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Fail("KEYCLOAK_ERROR", 
                    "Falha na criação do usuário no Keycloak. Tente novamente."));
            }

            // Buscar o usuário criado para retornar os dados completos
            var createdUser = await _keycloakService.GetUserByEmailAsync(request.Email);
            if (createdUser == null)
            {
                _logger.LogError("Usuário criado no Keycloak mas não foi possível recuperar os dados: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Fail("KEYCLOAK_ERROR", 
                    "Usuário criado mas não foi possível recuperar os dados."));
            }

            var response = new CreateKeycloakUserResponse(
                Id: createdUser.Id,
                Username: createdUser.Username,
                Email: createdUser.Email,
                FirstName: createdUser.FirstName,
                LastName: createdUser.LastName,
                Enabled: createdUser.Enabled,
                EmailVerified: createdUser.EmailVerified,
                Roles: createdUser.Roles,
                CreatedAt: DateTimeOffset.FromUnixTimeMilliseconds(createdUser.CreatedTimestamp).DateTime
            );

            _logger.LogInformation("Usuário criado com sucesso no Keycloak. ID: {UserId}, Email: {Email}", 
                userId, request.Email);

            return CreatedAtAction(nameof(CreateUser), new { id = userId }, 
                ApiResponse<CreateKeycloakUserResponse>.Ok(response));
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Dados inválidos para criação de usuário no Keycloak: {Email}", request.Email);
            return BadRequest(ApiResponse.Fail("VALIDATION_ERROR", argEx.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar usuário no Keycloak: {Email}", request.Email);
            return StatusCode(500, ApiResponse.Fail("INTERNAL_ERROR", 
                "Erro interno do servidor. Tente novamente."));
        }
    }

    /// <summary>
    /// Endpoint para buscar um usuário no Keycloak por email
    /// </summary>
    /// <param name="email">Email do usuário a ser buscado</param>
    /// <returns>Dados do usuário encontrado</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="404">Usuário não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("users/{email}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseKeycloak>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            _logger.LogInformation("Buscando usuário no Keycloak por email: {Email}", email);

            var user = await _keycloakService.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation("Usuário não encontrado no Keycloak: {Email}", email);
                return NotFound(ApiResponse.Fail("USER_NOT_FOUND", 
                    $"Usuário com email {email} não foi encontrado no Keycloak"));
            }

            _logger.LogInformation("Usuário encontrado no Keycloak: {Email}", email);
            return Ok(ApiResponse<UserResponseKeycloak>.Ok(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário no Keycloak: {Email}", email);
            return StatusCode(500, ApiResponse.Fail("INTERNAL_ERROR", 
                "Erro interno do servidor. Tente novamente."));
        }
    }

    /// <summary>
    /// Endpoint para deletar um usuário no Keycloak
    /// </summary>
    /// <param name="userId">ID do usuário a ser deletado</param>
    /// <returns>Confirmação da exclusão</returns>
    /// <response code="204">Usuário deletado com sucesso</response>
    /// <response code="404">Usuário não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            _logger.LogInformation("Deletando usuário no Keycloak: {UserId}", userId);

            var success = await _keycloakService.DeleteUserAsync(userId);
            if (!success)
            {
                _logger.LogWarning("Falha ao deletar usuário no Keycloak: {UserId}", userId);
                return NotFound(ApiResponse.Fail("USER_NOT_FOUND", 
                    $"Usuário com ID {userId} não foi encontrado no Keycloak"));
            }

            _logger.LogInformation("Usuário deletado com sucesso no Keycloak: {UserId}", userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar usuário no Keycloak: {UserId}", userId);
            return StatusCode(500, ApiResponse.Fail("INTERNAL_ERROR", 
                "Erro interno do servidor. Tente novamente."));
        }
    }
}