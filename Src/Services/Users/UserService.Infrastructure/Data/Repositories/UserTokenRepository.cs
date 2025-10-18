using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Data.Repositories;

public class UserTokenRepository : IUserTokenRepository
{
    private readonly UserServiceDbContext _context;

    public UserTokenRepository(UserServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenId == id && t.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar token por ID: {id}", ex);
        }
    }

    public async Task<IEnumerable<UserToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserTokens
                .Include(t => t.User)
                .Where(t => t.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar todos os tokens", ex);
        }
    }

    public async Task AddAsync(UserToken entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            await _context.UserTokens.AddAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao adicionar token", ex);
        }
    }

    public void Update(UserToken entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.UserTokens.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao atualizar token", ex);
        }
    }

    public void Delete(UserToken entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // Soft delete
            entity.SoftDelete();
            _context.UserTokens.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao deletar token", ex);
        }
    }

    // Métodos específicos para UserToken
    public async Task<UserToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new ArgumentException("Token value não pode ser nulo ou vazio", nameof(tokenValue));

        try
        {
            return await _context.UserTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenValue == tokenValue && t.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar token por valor: {tokenValue}", ex);
        }
    }

    public async Task<IEnumerable<UserToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.UserTokens
                .Include(t => t.User)
                .Where(t => t.UserId == userId 
                           && t.DeletedAt == null 
                           && t.RevokedAt == null 
                           && t.ExpiresAt > now)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar tokens ativos do usuário: {userId}", ex);
        }
    }

    public async Task<IEnumerable<UserToken>> GetTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserTokens
                .Include(t => t.User)
                .Where(t => t.UserId == userId && t.DeletedAt == null)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar tokens do usuário: {userId}", ex);
        }
    }

    public async Task<IEnumerable<UserToken>> GetTokensByTypeAsync(UserTokenType tokenType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserTokens
                .Include(t => t.User)
                .Where(t => t.TokenType == tokenType && t.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar tokens por tipo: {tokenType}", ex);
        }
    }

    public async Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.TokenId == tokenId && t.DeletedAt == null, cancellationToken);

            if (token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                _context.UserTokens.Update(token);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao revogar token: {tokenId}", ex);
        }
    }

    public async Task RevokeTokenByValueAsync(string tokenValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new ArgumentException("Token value não pode ser nulo ou vazio", nameof(tokenValue));

        try
        {
            var token = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.TokenValue == tokenValue && t.DeletedAt == null, cancellationToken);

            if (token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                _context.UserTokens.Update(token);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao revogar token por valor: {tokenValue}", ex);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await _context.UserTokens
                .Where(t => t.UserId == userId && t.DeletedAt == null && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }

            if (tokens.Any())
            {
                _context.UserTokens.UpdateRange(tokens);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao revogar todos os tokens do usuário: {userId}", ex);
        }
    }

    public async Task<IEnumerable<UserToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.UserTokens
                .Include(t => t.User)
                .Where(t => t.ExpiresAt <= now && t.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar tokens expirados", ex);
        }
    }

    public async Task<bool> IsTokenValidAsync(string tokenValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
            return false;

        try
        {
            var now = DateTime.UtcNow;
            return await _context.UserTokens
                .AnyAsync(t => t.TokenValue == tokenValue 
                              && t.DeletedAt == null 
                              && t.RevokedAt == null 
                              && t.ExpiresAt > now, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao verificar validade do token: {tokenValue}", ex);
        }
    }

    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredTokens = await _context.UserTokens
                .Where(t => t.ExpiresAt <= now && t.DeletedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in expiredTokens)
            {
                token.SoftDelete();
            }

            if (expiredTokens.Any())
            {
                _context.UserTokens.UpdateRange(expiredTokens);
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao limpar tokens expirados", ex);
        }
    }
}