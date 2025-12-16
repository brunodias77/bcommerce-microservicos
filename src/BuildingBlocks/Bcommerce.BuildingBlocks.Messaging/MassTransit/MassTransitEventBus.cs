using Bcommerce.BuildingBlocks.Messaging.Abstractions;
using MassTransit;

namespace Bcommerce.BuildingBlocks.Messaging.MassTransit;

public class MassTransitEventBus : IEventBus, IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(message, messageType, cancellationToken);
    }
}
