using BuildingBlocks.Data;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Domain.ValueObjects;

namespace UserService.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserServiceDbContext _context;

    public UserRepository(UserServiceDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .FirstOrDefaultAsync(u => u.UserId == id && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário por ID: {id}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .Where(u => u.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar todos os usuários", ex);
        }
    }

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            await _context.Users.AddAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao adicionar usuário", ex);
        }
    }

    public void Update(User entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            _context.Users.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao atualizar usuário", ex);
        }
    }

    public void Delete(User entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // Soft delete
            entity.SoftDelete();
            _context.Users.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao deletar usuário", ex);
        }
    }

    // Métodos específicos para User
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário por email: {email.Value}", ex);
        }
    }

    public async Task<User?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .FirstOrDefaultAsync(u => u.KeyCloakId == keycloakId && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário por Keycloak ID: {keycloakId}", ex);
        }
    }

    public async Task<User?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        if (cpf == null)
            throw new ArgumentNullException(nameof(cpf));

        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .FirstOrDefaultAsync(u => u.Cpf == cpf && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário por CPF: {cpf.Value}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .Where(u => u.Status == UserStatus.Ativo && u.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar usuários ativos", ex);
        }
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        if (email == null)
            throw new ArgumentNullException(nameof(email));

        try
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao verificar existência de usuário por email: {email.Value}", ex);
        }
    }

    public async Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default)
    {
        if (cpf == null)
            throw new ArgumentNullException(nameof(cpf));

        try
        {
            return await _context.Users
                .AnyAsync(u => u.Cpf == cpf && u.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao verificar existência de usuário por CPF: {cpf.Value}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.SavedCards)
                .Include(u => u.Tokens)
                .Include(u => u.Consents)
                .Where(u => u.Role == role && u.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuários por role: {role}", ex);
        }
    }
}