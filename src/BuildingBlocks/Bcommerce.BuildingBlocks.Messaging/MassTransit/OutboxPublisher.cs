using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.BuildingBlocks.Infrastructure.Outbox;
using Bcommerce.BuildingBlocks.Messaging.Abstractions;
using Newtonsoft.Json;

namespace Bcommerce.BuildingBlocks.Messaging.MassTransit;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly IEventBus _eventBus;

    public OutboxPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : class, IDomainEvent
    {
        // This publisher is expected to be used by the OutboxProcessor worker.
        // It takes the domain event (which might have been deserialized from OutboxMessage) 
        // and publishes it to the service bus.
        
        await _eventBus.PublishAsync(domainEvent, cancellationToken);
    }
}
