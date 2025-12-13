namespace Common.Domain.Repositories;

/// <summary>
/// Interface para Unit of Work pattern
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Salva todas as mudanças no banco de dados
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva mudanças e publica eventos de domínio
    /// </summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
