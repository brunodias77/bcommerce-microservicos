using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade SavedCard para Entity Framework
/// </summary>
public class SavedCardConfiguration : IEntityTypeConfiguration<SavedCard>
{
    public void Configure(EntityTypeBuilder<SavedCard> builder)
    {
        // Configuração da tabela
        builder.ToTable("user_saved_cards");

        // Chave primária
        builder.HasKey(sc => sc.SavedCardId);
        builder.Property(sc => sc.SavedCardId)
            .HasColumnName("saved_card_id")
            .IsRequired();

        // Propriedades
        builder.Property(sc => sc.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(sc => sc.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(50);

        builder.Property(sc => sc.LastFourDigits)
            .HasColumnName("last_four_digits")
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(sc => sc.Brand)
            .HasColumnName("brand")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sc => sc.GatewayToken)
            .HasColumnName("gateway_token")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(sc => sc.ExpiryDate)
            .HasColumnName("expiry_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(sc => sc.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        // Configuração de herança da Entity
        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at");

        // Relacionamentos
        builder.HasOne(sc => sc.User)
            .WithMany(u => u.SavedCards)
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(sc => sc.UserId)
            .HasDatabaseName("IX_user_saved_cards_user_id");

        builder.HasIndex(sc => new { sc.UserId, sc.IsDefault })
            .HasDatabaseName("IX_user_saved_cards_user_id_is_default")
            .HasFilter("is_default = true");

        builder.HasIndex(sc => sc.GatewayToken)
            .IsUnique()
            .HasDatabaseName("IX_user_saved_cards_gateway_token");
    }
}