using Microsoft.Extensions.Logging;
using System.Text.Json;

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
                    "Processing outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);

                // Desserializa o payload do evento
                var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Payload);

                // Publica no Event Bus
                await _eventBus.PublishAsync(
                    message.EventType,
                    eventData!,
                    cancellationToken);

                // Marca como processado
                await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId}",
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId}",
                    message.Id);

                await _outboxRepository.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    cancellationToken);
            }
        }
    }
}

// Interface temporária para IEventBus (será criada depois)
public interface IEventBus
{
    Task PublishAsync(string eventType, object eventData, CancellationToken cancellationToken);
}