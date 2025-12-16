using Bcommerce.BuildingBlocks.Infrastructure.Data;

namespace Bcommerce.BuildingBlocks.Infrastructure.AuditLog;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly BaseDbContext _context;

    public AuditLogRepository(BaseDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log)
    {
        await _context.Set<AuditLog>().AddAsync(log);
    }
}
