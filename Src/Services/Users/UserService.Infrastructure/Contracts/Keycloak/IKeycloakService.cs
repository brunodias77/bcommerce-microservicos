using UserService.Infrastructure.Dtos.Keycloak;

namespace UserService.Application.Contracts.Keycloak;

public interface IKeycloakService
{
    Task<string> CreateUserAsync(CreateUserKeycloak request);
    Task<UserResponseKeycloak?> GetUserByEmailAsync(string email);
    Task<bool> DeleteUserAsync(string userId);

}