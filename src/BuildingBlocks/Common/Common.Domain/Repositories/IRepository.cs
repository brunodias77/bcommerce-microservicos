using Common.Domain.Entities;

namespace Common.Domain.Repositories;

/// <summary>
/// Interface genérica para repositórios
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity, IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> specification, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity?> FindOneAsync(
        ISpecification<TEntity> specification, 
        CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(
        ISpecification<TEntity>? specification = null, 
        CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(
        ISpecification<TEntity> specification, 
        CancellationToken cancellationToken = default);

    TEntity Add(TEntity entity);
    
    void Update(TEntity entity);
    
    void Delete(TEntity entity);
    
    void DeleteRange(IEnumerable<TEntity> entities);
}