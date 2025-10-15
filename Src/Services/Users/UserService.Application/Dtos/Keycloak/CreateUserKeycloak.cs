namespace UserService.Application.Dtos.Keycloak;

public record CreateUserKeycloak(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    bool Enabled = true,
    bool EmailVerified = false,
    List<string> Roles = null!
);