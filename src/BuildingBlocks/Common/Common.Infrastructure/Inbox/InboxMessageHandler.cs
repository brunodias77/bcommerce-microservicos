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
                "Mensagem {MessageId} do tipo {MessageType} já processada. Pulando.",
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
                "Mensagem {MessageId} do tipo {MessageType} processada com sucesso",
                messageId,
                messageType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar mensagem {MessageId} do tipo {MessageType}",
                messageId,
                messageType);

            throw;
        }
    }
}
