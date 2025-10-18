namespace UserService.Infrastructure.Dtos.Keycloak;

public record LoginUserKeycloak(
    string Email,
    string Password
);