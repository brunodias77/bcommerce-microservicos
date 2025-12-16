using MediatR;

namespace Bcommerce.BuildingBlocks.Messaging.Abstractions;

public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
