using MediatR;

namespace Bcommerce.BuildingBlocks.Core.Domain;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
