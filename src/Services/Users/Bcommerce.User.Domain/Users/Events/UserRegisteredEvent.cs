using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.User.Domain.Users.Events;

public record UserRegisteredEvent(Guid UserId, string Email, string FirstName, string LastName) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}
