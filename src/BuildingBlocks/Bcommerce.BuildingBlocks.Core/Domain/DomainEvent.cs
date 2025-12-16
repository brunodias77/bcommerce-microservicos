namespace Bcommerce.BuildingBlocks.Core.Domain;

public abstract record DomainEvent(Guid EventId, DateTime OccurredOn) : IDomainEvent
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
