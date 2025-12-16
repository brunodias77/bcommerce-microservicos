using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.BuildingBlocks.Infrastructure.Outbox;

public interface IOutboxPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : class, IDomainEvent;
}
