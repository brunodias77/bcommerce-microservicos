using Bcommerce.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.Catalog.Infrastructure.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).IsRequired();
        builder.Property(i => i.AltText).HasMaxLength(255);
        builder.Property(i => i.IsPrimary).HasDefaultValue(false);
        builder.Property(i => i.SortOrder).HasDefaultValue(0);
        
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()");
    }
}
