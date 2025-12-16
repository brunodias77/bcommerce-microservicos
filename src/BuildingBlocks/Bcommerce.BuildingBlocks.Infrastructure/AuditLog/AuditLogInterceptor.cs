using Bcommerce.BuildingBlocks.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace Bcommerce.BuildingBlocks.Infrastructure.AuditLog;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);
        
        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<Entity<Guid>>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                // TODO: Get real user ID from context service
                UserId = "System", 
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                Action = entry.State.ToString()
            };

            if (entry.State == EntityState.Modified)
            {
                // Simple serialization for demo
                // In production, compare OriginalValues vs CurrentValues
                auditEntry.OldValues = "{}"; 
                auditEntry.NewValues = JsonConvert.SerializeObject(entry.Entity);
            }
            else if (entry.State == EntityState.Added)
            {
                auditEntry.NewValues = JsonConvert.SerializeObject(entry.Entity);
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditEntry.OldValues = JsonConvert.SerializeObject(entry.Entity);
            }

            auditEntries.Add(auditEntry);
        }

        // Warning: Modifying context in interceptor can be tricky if not careful
        // Usually we would just add to a separate DbSet or save directly.
        // For now, let's assume valid implementation involves adding to context if it has DbSet<AuditLog> definitions
        // or using raw SQL if strictly separated.
        // This is a simplified example.
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
