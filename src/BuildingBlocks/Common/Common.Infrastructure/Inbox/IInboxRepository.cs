namespace Common.Infrastructure.Inbox;

/// <summary>
/// Interface para repositório de mensagens do Inbox
/// </summary>
public interface IInboxRepository
{
    /// <summary>
    /// Verifica se uma mensagem já foi processada
    /// </summary>
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma mensagem ao inbox
    /// </summary>
    Task AddAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona se não existir (idempotente)
    /// </summary>
    Task<bool> TryAddAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default);
}