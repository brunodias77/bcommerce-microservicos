using UserService.Application.Dtos.Keycloak;

namespace UserService.Application.Contracts.Keycloak;

public interface IKeycloakService
{
    Task<string> CreateUserAsync(CreateUserKeycloak request);
    Task<UserResponseKeycloak?> GetUserByEmailAsync(string email);
    Task<UserResponseKeycloak?> GetUserByIdAsync(string userId);
    Task<bool> DeleteUserAsync(string userId);
    Task<LoginResponseKeycloak> LoginAsync(LoginUserKeycloak request);
    Task<bool> SendEmailVerificationAsync(string userId);
    Task<LoginResponseKeycloak> RefreshTokenAsync(string refreshToken);
    Task<bool> ResetPasswordAsync(ResetPasswordKeycloak request);
    Task<bool> UpdatePasswordAsync(string userId, string newPassword);
}