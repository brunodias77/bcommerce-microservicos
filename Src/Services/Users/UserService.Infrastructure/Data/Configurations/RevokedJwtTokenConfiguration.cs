using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade RevokedJwtToken para Entity Framework
/// </summary>
public class RevokedJwtTokenConfiguration : IEntityTypeConfiguration<RevokedJwtToken>
{
    public void Configure(EntityTypeBuilder<RevokedJwtToken> builder)
    {
        // Configuração da tabela
        builder.ToTable("revoked_jwt_tokens");

        // Chave primária
        builder.HasKey(rjt => rjt.Jti);
        builder.Property(rjt => rjt.Jti)
            .HasColumnName("jti")
            .IsRequired();

        // Propriedades
        builder.Property(rjt => rjt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rjt => rjt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        // Ignorar propriedades da Entity base que não existem na tabela
        builder.Ignore(rjt => rjt.CreatedAt);
        builder.Ignore(rjt => rjt.UpdatedAt);
        builder.Ignore(rjt => rjt.DeletedAt);
        builder.Ignore(rjt => rjt.Version);
        builder.Ignore(rjt => rjt.DomainEvents);

        // Relacionamentos
        builder.HasOne(rjt => rjt.User)
            .WithMany(u => u.RevokedTokens)
            .HasForeignKey(rjt => rjt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(rjt => rjt.UserId)
            .HasDatabaseName("IX_revoked_jwt_tokens_user_id");

        builder.HasIndex(rjt => rjt.ExpiresAt)
            .HasDatabaseName("IX_revoked_jwt_tokens_expires_at");
    }
}