using Common.Domain.Entities;
using Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Persistence;

/// <summary>
/// Implementação base do Repository Pattern
/// </summary>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity>
    where TEntity : Entity, IAggregateRoot
{
    protected readonly DbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected RepositoryBase(DbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public IUnitOfWork UnitOfWork => (IUnitOfWork)Context;

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FindOneAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default)
    {
        if (specification == null)
            return await DbSet.CountAsync(cancellationToken);

        var query = SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification);
        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification);
        return await query.AnyAsync(cancellationToken);
    }

    public virtual TEntity Add(TEntity entity)
    {
        return DbSet.Add(entity).Entity;
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(TEntity entity)
    {
        // Soft delete
        if (entity is IAggregateRoot aggregate)
        {
            aggregate.Delete();
            DbSet.Update(entity);
        }
        else
        {
            DbSet.Remove(entity);
        }
    }

    public virtual void DeleteRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            Delete(entity);
        }
    }
}