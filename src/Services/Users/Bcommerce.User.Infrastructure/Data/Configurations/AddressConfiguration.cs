using Bcommerce.User.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.User.Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).HasMaxLength(50);
        builder.Property(a => a.RecipientName).HasMaxLength(150);
        builder.Property(a => a.Street).HasMaxLength(255).IsRequired();
        builder.Property(a => a.Number).HasMaxLength(20);
        builder.Property(a => a.Complement).HasMaxLength(100);
        builder.Property(a => a.Neighborhood).HasMaxLength(100);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(2).IsRequired();
        
        // Value Object: PostalCode
        builder.OwnsOne(a => a.PostalCode, postalCode =>
        {
            postalCode.Property(p => p.Code)
                .HasColumnName("postal_code")
                .HasMaxLength(9)
                .IsRequired();
        });

        builder.Property(a => a.Country).HasMaxLength(2).HasDefaultValue("BR");
        builder.Property(a => a.IsDefault).HasDefaultValue(false);
        builder.Property(a => a.IsBillingAddress).HasDefaultValue(false);

        // Timestamps
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
