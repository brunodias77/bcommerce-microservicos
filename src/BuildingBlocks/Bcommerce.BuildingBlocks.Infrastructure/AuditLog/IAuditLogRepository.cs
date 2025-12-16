namespace Bcommerce.BuildingBlocks.Infrastructure.AuditLog;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}
