namespace UserService.Infrastructure.Dtos.Keycloak;

public record ChangePasswordKeycloak(
    string CurrentPassword,
    string NewPassword
);