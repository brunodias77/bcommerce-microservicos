using Bcommerce.BuildingBlocks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.BuildingBlocks.Infrastructure.Inbox;

public class InboxMessageRepository : IInboxMessageRepository
{
    private readonly BaseDbContext _context;

    public InboxMessageRepository(BaseDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InboxMessage message)
    {
        await _context.Set<InboxMessage>().AddAsync(message);
    }

    public async Task<List<InboxMessage>> GetUnprocessedMessagesAsync(int batchSize)
    {
        return await _context.Set<InboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task UpdateAsync(InboxMessage message)
    {
        _context.Set<InboxMessage>().Update(message);
        await _context.SaveChangesAsync();
    }
}
