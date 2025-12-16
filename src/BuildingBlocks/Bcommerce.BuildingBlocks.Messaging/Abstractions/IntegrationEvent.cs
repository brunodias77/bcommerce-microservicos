namespace Bcommerce.BuildingBlocks.Messaging.Abstractions;

public abstract record IntegrationEvent(Guid EventId, DateTime OccurredOn) : IIntegrationEvent
{
    protected IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
