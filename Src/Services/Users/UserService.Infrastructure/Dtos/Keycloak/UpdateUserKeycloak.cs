namespace UserService.Infrastructure.Dtos.Keycloak;

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    bool? Enabled,
    List<string>? Roles
);