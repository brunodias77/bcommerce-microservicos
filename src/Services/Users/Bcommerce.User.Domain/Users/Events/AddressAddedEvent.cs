using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.User.Domain.ValueObjects;

namespace Bcommerce.User.Domain.Users.Events;

public record AddressAddedEvent(Guid UserId, Guid AddressId, string Street, string Number, string City, string State, string PostalCode) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}
