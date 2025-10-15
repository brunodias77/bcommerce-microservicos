namespace UserService.Application.Dtos.Keycloak;

public record ChangePasswordKeycloak(
    string CurrentPassword,
    string NewPassword
);