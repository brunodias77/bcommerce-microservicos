using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade UserToken para Entity Framework
/// </summary>
public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        // Configuração da tabela
        builder.ToTable("user_tokens");

        // Chave primária
        builder.HasKey(ut => ut.TokenId);
        builder.Property(ut => ut.TokenId)
            .HasColumnName("token_id")
            .IsRequired();

        // Propriedades
        builder.Property(ut => ut.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ut => ut.TokenType)
            .HasColumnName("token_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ut => ut.TokenValue)
            .HasColumnName("token_value")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(ut => ut.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(ut => ut.RevokedAt)
            .HasColumnName("revoked_at");

        // Configuração de herança da Entity
        builder.Property(ut => ut.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ut => ut.UpdatedAt)
            .HasColumnName("updated_at");

        // Relacionamentos
        builder.HasOne(ut => ut.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(ut => ut.UserId)
            .HasDatabaseName("IX_user_tokens_user_id");

        builder.HasIndex(ut => new { ut.TokenType, ut.ExpiresAt })
            .HasDatabaseName("IX_user_tokens_type_expires_at");

        builder.HasIndex(ut => ut.TokenValue)
            .IsUnique()
            .HasDatabaseName("IX_user_tokens_token_value");

        builder.HasIndex(ut => ut.ExpiresAt)
            .HasDatabaseName("IX_user_tokens_expires_at");
    }
}