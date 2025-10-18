using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Contracts.Keycloak;
using UserService.Domain.Exceptions;
using UserService.Infrastructure.Dtos.Keycloak;

namespace UserService.Infrastructure.Services.Keycloak;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakService> _logger;
    private readonly KeycloakSettings _keycloakSettings;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _adminToken;
    private DateTime _tokenExpiration = DateTime.MinValue;

    public KeycloakService(
        HttpClient httpClient,
        ILogger<KeycloakService> logger,
        IOptions<KeycloakSettings> keycloakSettings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keycloakSettings = keycloakSettings?.Value ?? throw new ArgumentNullException(nameof(keycloakSettings));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> CreateUserAsync(CreateUserKeycloak request)
    {
        _logger.LogInformation("Iniciando criação de usuário no Keycloak para email: {Email}", request.Email);
        
        try
        {
            // Obter token de administrador
            await EnsureAdminTokenAsync();

            // Preparar dados do usuário para o Keycloak
            var keycloakUser = new KeycloakUserRepresentation(
                Id: null,
                Username: request.Username,
                Email: request.Email,
                FirstName: request.FirstName,
                LastName: request.LastName,
                Enabled: request.Enabled,
                EmailVerified: request.EmailVerified,
                CreatedTimestamp: null,
                Credentials: new List<KeycloakCredential>
                {
                    new KeycloakCredential("password", request.Password, false)
                },
                Attributes: null
            );

            var json = JsonSerializer.Serialize(keycloakUser, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Fazer requisição para criar usuário
            var response = await _httpClient.PostAsync($"/admin/realms/{_keycloakSettings.Realm}/users", content);

            if (response.IsSuccessStatusCode)
            {
                // Extrair ID do usuário do header Location
                var locationHeader = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(locationHeader))
                {
                    var userId = locationHeader.Split('/').Last();
                    _logger.LogInformation("Usuário criado com sucesso no Keycloak. ID: {UserId}", userId);
                    
                    // Atribuir roles se especificadas
                    if (request.Roles?.Any() == true)
                    {
                        await AssignRolesToUserAsync(userId, request.Roles);
                    }
                    
                    return userId;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao criar usuário no Keycloak. Status: {StatusCode}, Erro: {Error}", 
                response.StatusCode, errorContent);
            
            throw KeycloakException.ForUserCreationError(request.Email, 
                $"Status: {response.StatusCode}, Detalhes: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao criar usuário no Keycloak: {Email}", request.Email);
            throw KeycloakException.ForConnectionError($"Falha na conexão: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is KeycloakException))
        {
            _logger.LogError(ex, "Erro inesperado ao criar usuário no Keycloak: {Email}", request.Email);
            throw KeycloakException.ForUserCreationError(request.Email, ex.Message, ex);
        }
    }

    public async Task<UserResponseKeycloak?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Buscando usuário no Keycloak por email: {Email}", email);
        
        try
        {
            // Obter token de administrador
            await EnsureAdminTokenAsync();

            // Buscar usuário por email
            var response = await _httpClient.GetAsync($"/admin/realms/{_keycloakSettings.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<KeycloakUserRepresentation>>(content, _jsonOptions);

                if (users?.Any() == true)
                {
                    var user = users.First();
                    _logger.LogInformation("Usuário encontrado no Keycloak: {UserId}", user.Id);
                    
                    // Buscar roles do usuário
                    var roles = await GetUserRolesAsync(user.Id!);
                    
                    return new UserResponseKeycloak(
                        Id: user.Id!,
                        Username: user.Username,
                        Email: user.Email,
                        FirstName: user.FirstName,
                        LastName: user.LastName,
                        Enabled: user.Enabled,
                        EmailVerified: user.EmailVerified,
                        CreatedTimestamp: user.CreatedTimestamp ?? 0,
                        Roles: roles,
                        Attributes: user.Attributes ?? new Dictionary<string, List<string>>()
                    );
                }
            }

            _logger.LogInformation("Usuário não encontrado no Keycloak: {Email}", email);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao buscar usuário no Keycloak: {Email}", email);
            throw KeycloakException.ForConnectionError($"Falha na conexão: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar usuário no Keycloak: {Email}", email);
            throw new KeycloakException($"Erro ao buscar usuário: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Deletando usuário no Keycloak: {UserId}", userId);
        
        try
        {
            // Obter token de administrador
            await EnsureAdminTokenAsync();

            // Deletar usuário
            var response = await _httpClient.DeleteAsync($"/admin/realms/{_keycloakSettings.Realm}/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Usuário deletado com sucesso no Keycloak: {UserId}", userId);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao deletar usuário no Keycloak. Status: {StatusCode}, Erro: {Error}", 
                response.StatusCode, errorContent);
            
            throw KeycloakException.ForUserDeletionError(userId, 
                $"Status: {response.StatusCode}, Detalhes: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao deletar usuário no Keycloak: {UserId}", userId);
            throw KeycloakException.ForConnectionError($"Falha na conexão: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is KeycloakException))
        {
            _logger.LogError(ex, "Erro inesperado ao deletar usuário no Keycloak: {UserId}", userId);
            throw KeycloakException.ForUserDeletionError(userId, ex.Message, ex);
        }
    }

    /// <summary>
    /// Garante que temos um token de administrador válido
    /// </summary>
    private async Task EnsureAdminTokenAsync()
    {
        if (string.IsNullOrEmpty(_adminToken) || DateTime.UtcNow >= _tokenExpiration)
        {
            await GetAdminTokenAsync();
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
    }

    /// <summary>
    /// Obtém token de administrador do Keycloak
    /// </summary>
    private async Task GetAdminTokenAsync()
    {
        _logger.LogDebug("Obtendo token de administrador do Keycloak");
        
        try
        {
            var tokenRequest = new KeycloakAdminTokenRequest(
                GrantType: "password",
                ClientId: "admin-cli",
                Username: _keycloakSettings.AdminUsername,
                Password: _keycloakSettings.AdminPassword
            );

            var formData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", tokenRequest.GrantType),
                new("client_id", tokenRequest.ClientId),
                new("username", tokenRequest.Username),
                new("password", tokenRequest.Password)
            };

            var content = new FormUrlEncodedContent(formData);
            // Use o realm master para autenticação de admin
            var response = await _httpClient.PostAsync($"/realms/master/protocol/openid-connect/token", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<LoginResponseKeycloak>(responseContent, _jsonOptions);

                if (tokenResponse != null)
                {
                    _adminToken = tokenResponse.AccessToken;
                    _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 60s de margem
                    _logger.LogDebug("Token de administrador obtido com sucesso");
                    return;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao obter token de administrador. Status: {StatusCode}, Erro: {Error}", 
                response.StatusCode, errorContent);
            
            throw KeycloakException.ForAuthenticationError(
                $"Falha na autenticação: Status {response.StatusCode}, Detalhes: {errorContent}");
        }
        catch (Exception ex) when (!(ex is KeycloakException))
        {
            _logger.LogError(ex, "Erro inesperado ao obter token de administrador");
            throw KeycloakException.ForAuthenticationError($"Erro na autenticação: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Atribui roles a um usuário
    /// </summary>
    private async Task AssignRolesToUserAsync(string userId, List<string> roleNames)
    {
        _logger.LogDebug("Atribuindo roles ao usuário {UserId}: {Roles}", userId, string.Join(", ", roleNames));
        
        try
        {
            // Buscar roles disponíveis no realm
            var availableRoles = await GetAvailableRolesAsync();
            var rolesToAssign = availableRoles.Where(r => roleNames.Contains(r.Name)).ToList();

            if (rolesToAssign.Any())
            {
                var json = JsonSerializer.Serialize(rolesToAssign, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"/admin/realms/{_keycloakSettings.Realm}/users/{userId}/role-mappings/realm", 
                    content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Roles atribuídas com sucesso ao usuário {UserId}", userId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao atribuir roles ao usuário {UserId}. Status: {StatusCode}, Erro: {Error}", 
                        userId, response.StatusCode, errorContent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atribuir roles ao usuário {UserId}", userId);
        }
    }

    /// <summary>
    /// Busca roles disponíveis no realm
    /// </summary>
    private async Task<List<KeycloakRoleRepresentation>> GetAvailableRolesAsync()
    {
        var response = await _httpClient.GetAsync($"/admin/realms/{_keycloakSettings.Realm}/roles");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<KeycloakRoleRepresentation>>(content, _jsonOptions) 
                   ?? new List<KeycloakRoleRepresentation>();
        }

        return new List<KeycloakRoleRepresentation>();
    }

    /// <summary>
    /// Busca roles de um usuário
    /// </summary>
    private async Task<List<string>> GetUserRolesAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/admin/realms/{_keycloakSettings.Realm}/users/{userId}/role-mappings/realm");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<List<KeycloakRoleRepresentation>>(content, _jsonOptions);
                return roles?.Select(r => r.Name).ToList() ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar roles do usuário {UserId}", userId);
        }

        return new List<string>();
    }
}