using Bcommerce.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.Catalog.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType)
            .HasConversion<string>() // Schema uses ENUM stock_movement_type_enum
            .HasColumnName("movement_type")
            .IsRequired();

        builder.Property(m => m.Quantity).IsRequired();
        builder.Property(m => m.StockBefore).IsRequired();
        builder.Property(m => m.StockAfter).IsRequired();
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.ReferenceId);

        builder.Property(m => m.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
