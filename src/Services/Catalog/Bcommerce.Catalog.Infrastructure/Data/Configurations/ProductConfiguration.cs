using Bcommerce.Catalog.Domain.Categories;
using Bcommerce.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.Catalog.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description);
        builder.Property(p => p.Slug).HasMaxLength(200).IsRequired();

        // SKU Value Object
        builder.OwnsOne(p => p.Sku, sku =>
        {
            sku.Property(s => s.Value)
               .HasColumnName("sku")
               .HasMaxLength(100)
               .IsRequired();
            sku.HasIndex(s => s.Value).IsUnique();
        });

        // Money Value Object
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("price")
                .HasPrecision(10, 2)
                .IsRequired();
            
            // Currency is usually static or stored, schema only has price decimal, implies default currency or single currency system.
            // Domain has Currency property. If not in DB, we ignore or map to constant?
            // Checking schema: price DECIMAL(10, 2) NOT NULL. No currency column.
            // We will ignore currency persistence for now or maps it to shadow property if needed, 
            // but since schema is strict, we'll just map Amount.
            price.Ignore(m => m.Currency);
        });

        // Dimensions Value Object
        builder.OwnsOne(p => p.Dimensions, dim =>
        {
            dim.Property(d => d.Height).HasColumnName("height_cm").HasColumnType("decimal(6,2)");
            dim.Property(d => d.Width).HasColumnName("width_cm").HasColumnType("decimal(6,2)");
            dim.Property(d => d.Length).HasColumnName("length_cm").HasColumnType("decimal(6,2)");
        });

        builder.Property(p => p.Stock).HasDefaultValue(0);
        builder.Property(p => p.ReservedStock).HasDefaultValue(0);
        
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasColumnType("varchar(50)") // Assuming mapping to string or enum type in PG. Schema uses enum type `product_status_enum`. 
            // Npgsql can map enums, but string conversion is safer for portability if not configuring NpgsqlDataSource.
            // Schema has `create type product_status_enum`. 
            // Ideally we use HasPostgresEnum, but that requires extra configuration in Context.
            // For now, let's stick to string conversion or see if Context handles it.
            // "product_status_enum" is the type name in DB.
            .HasColumnName("status");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reviews)
            .WithOne()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.Ignore(p => p.DomainEvents);
    }
}
