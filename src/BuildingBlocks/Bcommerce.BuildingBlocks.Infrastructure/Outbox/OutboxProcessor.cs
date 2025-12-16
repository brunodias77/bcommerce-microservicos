using Bcommerce.BuildingBlocks.Core.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bcommerce.BuildingBlocks.Infrastructure.Outbox;

public class OutboxProcessor
{
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IOutboxMessageRepository outboxRepository, IPublisher publisher, ILogger<OutboxProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.GetUnprocessedMessagesAsync(20);

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(message.Content, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });

                if (domainEvent != null)
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Erro ao processar mensagem do outbox: {MessageId}", message.Id);
            }
            
            await _outboxRepository.UpdateAsync(message);
        }
    }
}
