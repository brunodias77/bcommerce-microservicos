namespace Bcommerce.BuildingBlocks.Infrastructure.Inbox;

public interface IInboxMessageRepository
{
    Task AddAsync(InboxMessage message);
    Task<List<InboxMessage>> GetUnprocessedMessagesAsync(int batchSize);
    Task UpdateAsync(InboxMessage message);
}
