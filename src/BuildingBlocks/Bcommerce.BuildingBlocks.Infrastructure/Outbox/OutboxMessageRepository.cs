using Bcommerce.BuildingBlocks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.BuildingBlocks.Infrastructure.Outbox;

public class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly BaseDbContext _context;

    public OutboxMessageRepository(BaseDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.Set<OutboxMessage>().AddAsync(message);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task UpdateAsync(OutboxMessage message)
    {
        _context.Set<OutboxMessage>().Update(message);
        // Note: Actual saving is typically done by UnitOfWork.CommitAsync in the background worker
        // but here we might want immediate update for the worker. 
        // For simplicity, we assume the caller handles saving or we add SaveChanges here.
        await _context.SaveChangesAsync();
    }
}
