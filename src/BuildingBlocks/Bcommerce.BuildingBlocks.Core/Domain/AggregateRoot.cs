namespace Bcommerce.BuildingBlocks.Core.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
}
