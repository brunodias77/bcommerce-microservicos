using Common.Application.Interfaces;
using Common.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Common.Domain.Entities;

namespace Common.Infrastructure.Persistence;

/// <summary>
/// DbContext base com funcionalidades comuns
/// </summary>
public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;

    protected BaseDbContext(
        DbContextOptions options,
        IMediator mediator,
        ICurrentUser currentUser,
        IDateTime dateTime) : base(options)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Atualiza campos de auditoria
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(_currentUser.UserId ?? Guid.Empty, _currentUser.IpAddress);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedBy(_currentUser.UserId ?? Guid.Empty, _currentUser.IpAddress);
                    break;

                case EntityState.Deleted:
                    // Soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.SetDeletedBy(_currentUser.UserId ?? Guid.Empty, _currentUser.IpAddress);
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events antes de salvar
        await DispatchDomainEventsAsync(cancellationToken);

        // Salva mudanças
        var result = await SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica Query Filters globais para soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(BaseDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IAggregateRoot
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }
}