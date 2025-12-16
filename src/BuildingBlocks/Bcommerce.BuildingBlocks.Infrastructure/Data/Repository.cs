using Bcommerce.BuildingBlocks.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.BuildingBlocks.Infrastructure.Data;

public class Repository<T> : IRepository<T> where T : class, IAggregateRoot
{
    protected readonly BaseDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(BaseDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IUnitOfWork UnitOfWork => _context;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }
    
    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
