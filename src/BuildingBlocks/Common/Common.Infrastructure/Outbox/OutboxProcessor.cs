using EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Outbox;

/// <summary>
/// Processador de mensagens do Outbox
/// </summary>
public class OutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository outboxRepository,
        IEventBus eventBus,
        ILogger<OutboxProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.GetUnprocessedMessagesAsync(100, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation(
                    "Processando mensagem do outbox {MessageId} de tipo {EventType}",
                    message.Id,
                    message.EventType);

                // Publica no Event Bus usando o método dinâmico
                await _eventBus.PublishDynamicAsync(
                    message.EventType,
                    message.Payload,
                    cancellationToken);

                // Marca como processado
                await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Mensagem do outbox {MessageId} processada com sucesso",
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar mensagem do outbox {MessageId}",
                    message.Id);

                await _outboxRepository.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    cancellationToken);
            }
        }
    }
}
