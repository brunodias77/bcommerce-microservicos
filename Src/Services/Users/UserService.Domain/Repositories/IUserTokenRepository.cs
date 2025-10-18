using BuildingBlocks.Data;
using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.Domain.Repositories;

public interface IUserTokenRepository : IRepository<UserToken>
{
    // Métodos específicos para UserToken
    Task<UserToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserToken>> GetTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserToken>> GetTokensByTypeAsync(UserTokenType tokenType, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken = default);
    Task RevokeTokenByValueAsync(string tokenValue, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(string tokenValue, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}