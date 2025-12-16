using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.User.Domain.Users.Events;

public record ProfileUpdatedEvent(Guid UserId, string DisplayName, string AvatarUrl) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}
