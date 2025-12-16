namespace Bcommerce.BuildingBlocks.Infrastructure.Outbox;

public interface IOutboxMessageRepository
{
    Task AddAsync(OutboxMessage message);
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize);
    Task UpdateAsync(OutboxMessage message);
}
