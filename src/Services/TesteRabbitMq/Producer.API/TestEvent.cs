using Bcommerce.BuildingBlocks.Messaging.Abstractions;

namespace TesteRabbitMq.Contracts;

public record TestEvent(Guid EventId, DateTime OccurredOn, string Message) : IntegrationEvent(EventId, OccurredOn);
