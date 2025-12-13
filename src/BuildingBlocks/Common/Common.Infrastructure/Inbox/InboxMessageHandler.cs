using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Inbox;

/// <summary>
/// Handler base para processar mensagens com idempotência via Inbox
/// </summary>
public abstract class InboxMessageHandler
{
    private readonly IInboxRepository _inboxRepository;
    private readonly ILogger _logger;

    protected InboxMessageHandler(
        IInboxRepository inboxRepository,
        ILogger logger)
    {
        _inboxRepository = inboxRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processa a mensagem garantindo idempotência
    /// </summary>
    protected async Task<bool> ProcessWithIdempotencyAsync(
        Guid messageId,
        string messageType,
        Func<Task> processAction,
        CancellationToken cancellationToken = default)
    {
        // Verifica se já foi processada
        if (await _inboxRepository.ExistsAsync(messageId, cancellationToken))
        {
            _logger.LogInformation(
                "Message {MessageId} of type {MessageType} already processed. Skipping.",
                messageId,
                messageType);

            return false;
        }

        try
        {
            // Processa a mensagem
            await processAction();

            // Registra no inbox
            await _inboxRepository.AddAsync(
                new InboxMessage(messageId, messageType),
                cancellationToken);

            _logger.LogInformation(
                "Successfully processed message {MessageId} of type {MessageType}",
                messageId,
                messageType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message {MessageId} of type {MessageType}",
                messageId,
                messageType);

            throw;
        }
    }
}