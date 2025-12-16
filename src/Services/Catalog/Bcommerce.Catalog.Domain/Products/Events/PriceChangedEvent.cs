using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Products.Events;

public record PriceChangedEvent(Guid ProductId, decimal NewPrice, decimal OldPrice) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}
