using Bcommerce.BuildingBlocks.Core.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.BuildingBlocks.Infrastructure.Data.EntityConfigurations;

public abstract class AggregateRootConfiguration<T> : EntityBaseConfiguration<T> where T : AggregateRoot<Guid>
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        // Aggregate specific configurations (e.g. Version for concurrency)
        // builder.Property(e => e.Version).IsConcurrencyToken();
        
        builder.Ignore(e => e.DomainEvents);
    }
}
