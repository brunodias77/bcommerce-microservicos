using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade UserConsent para Entity Framework
/// </summary>
public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        // Configuração da tabela
        builder.ToTable("user_consents");

        // Chave primária
        builder.HasKey(uc => uc.ConsentId);
        builder.Property(uc => uc.ConsentId)
            .HasColumnName("consent_id")
            .IsRequired();

        // Propriedades
        builder.Property(uc => uc.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(uc => uc.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(uc => uc.TermsVersion)
            .HasColumnName("terms_version")
            .HasMaxLength(30);

        builder.Property(uc => uc.IsGranted)
            .HasColumnName("is_granted")
            .IsRequired();

        // Configuração de herança da Entity
        builder.Property(uc => uc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(uc => uc.UpdatedAt)
            .HasColumnName("updated_at");

        // Relacionamentos
        builder.HasOne(uc => uc.User)
            .WithMany(u => u.Consents)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(uc => uc.UserId)
            .HasDatabaseName("IX_user_consents_user_id");

        builder.HasIndex(uc => new { uc.UserId, uc.Type })
            .IsUnique()
            .HasDatabaseName("IX_user_consents_user_id_type");

        builder.HasIndex(uc => uc.Type)
            .HasDatabaseName("IX_user_consents_type");
    }
}