namespace Common.Infrastructure.Outbox;

/// <summary>
/// Interface para repositório de mensagens do Outbox
/// </summary>
public interface IOutboxRepository
{
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);
}
