namespace UserService.Application.Dtos.Keycloak;

public record LoginUserKeycloak(
    string Email,
    string Password
);