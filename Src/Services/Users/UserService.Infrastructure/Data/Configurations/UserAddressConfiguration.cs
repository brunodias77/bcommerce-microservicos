using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade UserAddress para Entity Framework
/// </summary>
public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        // Configuração da tabela
        builder.ToTable("user_addresses");

        // Chave primária
        builder.HasKey(ua => ua.AddressId);
        builder.Property(ua => ua.AddressId)
            .HasColumnName("address_id")
            .IsRequired();

        // Propriedades
        builder.Property(ua => ua.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ua => ua.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(ua => ua.Street)
            .HasColumnName("street")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(ua => ua.StreetNumber)
            .HasColumnName("street_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ua => ua.Complement)
            .HasColumnName("complement")
            .HasMaxLength(100);

        builder.Property(ua => ua.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ua => ua.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ua => ua.StateCode)
            .HasColumnName("state_code")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(ua => ua.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .HasDefaultValue("BR")
            .IsRequired();

        builder.Property(ua => ua.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        // Configuração do Enum
        builder.Property(ua => ua.Type)
            .HasConversion<string>()
            .HasColumnName("type")
            .HasMaxLength(20)
            .IsRequired();

        // Configuração de herança da Entity
        builder.Property(ua => ua.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ua => ua.UpdatedAt)
            .HasColumnName("updated_at");

        // Relacionamentos
        builder.HasOne(ua => ua.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(ua => ua.UserId)
            .HasDatabaseName("IX_user_addresses_user_id");

        builder.HasIndex(ua => new { ua.UserId, ua.IsDefault })
            .HasDatabaseName("IX_user_addresses_user_id_is_default")
            .HasFilter("is_default = true");

        builder.HasIndex(ua => ua.PostalCode)
            .HasDatabaseName("IX_user_addresses_postal_code");
    }
}