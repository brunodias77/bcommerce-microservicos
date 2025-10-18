namespace UserService.Api.DTOs.Keycloak;

/// <summary>
/// DTO para resposta de criação de usuário no Keycloak
/// </summary>
public record CreateKeycloakUserResponse(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Enabled,
    bool EmailVerified,
    List<string> Roles,
    DateTime CreatedAt
);