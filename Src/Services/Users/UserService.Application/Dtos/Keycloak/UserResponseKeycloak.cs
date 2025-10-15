namespace UserService.Application.Dtos.Keycloak;

public record UserResponseKeycloak(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Enabled,
    bool EmailVerified,
    long CreatedTimestamp,
    List<string> Roles,
    Dictionary<string, List<string>> Attributes
);