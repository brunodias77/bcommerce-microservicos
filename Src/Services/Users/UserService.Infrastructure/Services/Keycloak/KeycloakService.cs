using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Contracts.Keycloak;
using UserService.Application.Dtos.Keycloak;
using UserService.Domain.Exceptions;

namespace UserService.Infrastructure.Services.Keycloak;

public class KeycloakService : IKeycloakService
{
     private readonly HttpClient _httpClient;
    private readonly KeycloakSettings _settings;
    private readonly ILogger<KeycloakService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KeycloakService(
        HttpClient httpClient,
        IOptions<KeycloakSettings> settings,
        ILogger<KeycloakService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }


    public async Task<string> CreateUserAsync(CreateUserKeycloak request)
    {
         try
        {
            var adminToken = await GetAdminTokenAsync();
            
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
                    new("password", request.Password, false)
                },
                Attributes: null
            );

            var json = JsonSerializer.Serialize(keycloakUser, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            
            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var location = response.Headers.Location?.ToString();
                var userId = location?.Split('/').LastOrDefault();
                
                if (!string.IsNullOrEmpty(userId) && request.Roles.Any())
                {
                    await AssignRolesToUserAsync(userId, request.Roles);
                }

                return userId ?? string.Empty;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao criar usuário {Username}: {Error}", request.Username, errorContent);
            throw KeycloakException.ForUserCreationError(request.Username, $"Erro ao criar usuário no Keycloak: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário {Username}", request.Username);
            throw;
        }
    }

    public async Task<UserResponseKeycloak?> GetUserByEmailAsync(string email)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Falha ao pesquisar usuário: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<KeycloakUserRepresentation>>(content, _jsonOptions);

            var user = users?.FirstOrDefault();
            if (user == null) return null;

            var roles = await GetUserRolesAsync(user.Id!);

            return MapToUserResponse(user, roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuário por e-mail {Email}", email);
            throw;
        }
    }

    public async Task<UserResponseKeycloak?> GetUserByIdAsync(string userId)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw new InvalidOperationException($"Falha ao obter o usuário {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var keycloakUser = JsonSerializer.Deserialize<KeycloakUserRepresentation>(content, _jsonOptions);

            if (keycloakUser == null) return null;

            var roles = await GetUserRolesAsync(userId);

            return MapToUserResponse(keycloakUser, roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuário {UserId}", userId);
            throw;
        }
        
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}";
            var response = await _httpClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw KeycloakException.ForUserDeletionError(userId, $"{response.StatusCode} - {errorContent}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (KeycloakException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar usuario {UserId}", userId);
            throw;
        }
    }

    public async Task<LoginResponseKeycloak> LoginAsync(LoginUserKeycloak request)
    {
        try
        {
            var tokenRequest = new KeycloakTokenRequest
            {
                ClientId = _settings.FrontendClientId,
                Username = request.Email,
                Password = request.Password
            };

            var content = CreateFormContent(tokenRequest);
            var url = $"{_settings.Url}/realms/{_settings.Realm}/protocol/openid-connect/token";

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<KeycloakErrorResponse>(responseContent, _jsonOptions);
                throw KeycloakException.ForAuthenticationError($"Erro ao fazer login no Keycloak: {error?.ErrorDescription ?? "Credenciais inválidas"}");
            }

            var loginResponse = JsonSerializer.Deserialize<LoginResponseKeycloak>(responseContent, _jsonOptions);
            return loginResponse ?? throw new InvalidOperationException("Falha ao desserializar a resposta de login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o login do usuário {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string userId)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);  
            
            
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Não é possível enviar verificação de e-mail: usuário {UserId} não encontrado", userId);
                return false;
            }
            
            if (user.EmailVerified)
            {
                _logger.LogInformation("E-mail já verificado para o usuário {UserId}", userId);
                return true;
            }
            
            // Enviar verificação de e-mail usando o endpoint execute-actions-email do Keycloak
            var actions = new[] { "VERIFY_EMAIL" };
            var json = JsonSerializer.Serialize(actions, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/execute-actions-email";
            var response = await _httpClient.PutAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Verificação de e-mail enviada com sucesso para o usuário {UserId}", userId);
                return true;
            }
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao enviar verificação de e-mail para o usuário {UserId}. Status: {StatusCode}, Error: {Error}", 
                userId, response.StatusCode, errorContent);
            return false;
        }catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail de verificação para o usuário {UserId}", userId);
            throw;
        }
    }

    public async Task<LoginResponseKeycloak> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", _settings.FrontendClientId),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });
            
            var url = $"{_settings.Url}/realms/{_settings.Realm}/protocol/openid-connect/token";
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<KeycloakErrorResponse>(responseContent, _jsonOptions);
                throw new UnauthorizedAccessException($"Falha na atualização do token: {error?.ErrorDescription}");
            }
            var loginResponse = JsonSerializer.Deserialize<LoginResponseKeycloak>(responseContent, _jsonOptions);
            return loginResponse ?? throw new InvalidOperationException("Falha ao desserializar a resposta de atualização");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar o token");
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordKeycloak request)
    {
        try
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null) return false;
            
            
            // Enviar e-mail para redefinir a senha
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            
            var actions = new[] { "UPDATE_PASSWORD" };
            var json = JsonSerializer.Serialize(actions, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{user.Id}/execute-actions-email";
            var response = await _httpClient.PutAsync(url, content);

            return response.IsSuccessStatusCode;
            
        }catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao redefinir a senha do usuário {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordAsync(string userId, string newPassword)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            
            var passwordData = new
            {
                type = "password",
                value = newPassword,
                temporary = false
            };
            
            var json = JsonSerializer.Serialize(passwordData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/reset-password";
            var response = await _httpClient.PutAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Senha atualizada com sucesso para o usuário {UserId}", userId);
                return true;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Falha ao atualizar senha para o usuário {UserId}. Status: {StatusCode}, Erro: {Error}", 
                userId, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar senha para o usuário {UserId}", userId);
            return false;
        }
    }

    private async Task<string> GetAdminTokenAsync()
    {
        try
        {
            var tokenRequest = new KeycloakAdminTokenRequest
            {
                Username = _settings.AdminUsername,
                Password = _settings.AdminPassword
            };

            var content = CreateFormContent(tokenRequest);
            var url = $"{_settings.Url}/realms/master/protocol/openid-connect/token";

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<KeycloakErrorResponse>(responseContent, _jsonOptions);
                throw new UnauthorizedAccessException($"Admin token request failed: {error?.ErrorDescription}");
            }

            var tokenResponse = JsonSerializer.Deserialize<LoginResponseKeycloak>(responseContent, _jsonOptions);
            return tokenResponse?.AccessToken ?? throw new InvalidOperationException("Failed to get admin token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin token");
            throw;
        }
    }
    
    
    private static FormUrlEncodedContent CreateFormContent(object obj)
    {
        var properties = obj.GetType().GetProperties();
        var keyValuePairs = new List<KeyValuePair<string, string>>();

        foreach (var property in properties)
        {
            var jsonPropertyName = property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .FirstOrDefault()?.Name ?? property.Name.ToLowerInvariant();

            var value = property.GetValue(obj)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                keyValuePairs.Add(new KeyValuePair<string, string>(jsonPropertyName, value));
            }
        }

        return new FormUrlEncodedContent(keyValuePairs);
    }
    
    public async Task<bool> AssignRolesToUserAsync(string userId, List<string> roles)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Get role representations
            var roleRepresentations = new List<KeycloakRoleRepresentation>();
            foreach (var roleName in roles)
            {
                var roleUrl = $"{_settings.Url}/admin/realms/{_settings.Realm}/roles/{roleName}";
                var roleResponse = await _httpClient.GetAsync(roleUrl);

                if (roleResponse.IsSuccessStatusCode)
                {
                    var roleContent = await roleResponse.Content.ReadAsStringAsync();
                    var role = JsonSerializer.Deserialize<KeycloakRoleRepresentation>(roleContent, _jsonOptions);
                    if (role != null)
                        roleRepresentations.Add(role);
                }
            }

            if (!roleRepresentations.Any()) return true;

            var json = JsonSerializer.Serialize(roleRepresentations, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/role-mappings/realm";
            var response = await _httpClient.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles to user {UserId}", userId);
            throw;
        }
    }
    
    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        try
        {
            var adminToken = await GetAdminTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var url = $"{_settings.Url}/admin/realms/{_settings.Realm}/users/{userId}/role-mappings/realm";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var content = await response.Content.ReadAsStringAsync();
            var roles = JsonSerializer.Deserialize<List<KeycloakRoleRepresentation>>(content, _jsonOptions);

            return roles?.Select(r => r.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter funções para o usuário {UserId}", userId);
            return new List<string>();
        }
    }
    
    private static UserResponseKeycloak MapToUserResponse(KeycloakUserRepresentation keycloakUser, List<string> roles)
    {
        return new UserResponseKeycloak
        (
            Id: keycloakUser.Id ?? string.Empty,
            Username: keycloakUser.Username,
            Email: keycloakUser.Email,
            FirstName: keycloakUser.FirstName,
            LastName: keycloakUser.LastName,
            Enabled: keycloakUser.Enabled,
            EmailVerified: keycloakUser.EmailVerified,
            CreatedTimestamp: keycloakUser.CreatedTimestamp ?? 0,
            Roles: roles,
            Attributes: keycloakUser.Attributes ?? new Dictionary<string, List<string>>()
        );
    }

}