using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Aggregates;
using UserService.Domain.ValueObjects;

namespace UserService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração da entidade User para Entity Framework
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configuração da tabela
        builder.ToTable("users");

        // Chave primária
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Propriedades básicas
        builder.Property(u => u.KeyCloakId)
            .HasColumnName("keycloak_id");

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(155)
            .IsRequired();

        builder.Property(u => u.EmailVerifiedAt)
            .HasColumnName("email_verified_at");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255);

        builder.Property(u => u.DateOfBirth)
            .HasColumnName("date_of_birth")
            .HasColumnType("date");

        builder.Property(u => u.NewsletterOptIn)
            .HasColumnName("newsletter_opt_in")
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0);

        builder.Property(u => u.AccountLockedUntil)
            .HasColumnName("account_locked_until");

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        // Configuração dos Value Objects
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? Phone.Create(value) : null)
            .HasColumnName("phone")
            .HasMaxLength(20);

        builder.Property(u => u.Cpf)
            .HasConversion(
                cpf => cpf != null ? cpf.Value : null,
                value => value != null ? Cpf.Create(value) : null)
            .HasColumnName("cpf")
            .HasMaxLength(11);

        // Configuração dos Enums
        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired();

        // Configuração de herança da AggregateRoot
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(u => u.Version)
            .HasColumnName("version")
            .IsRequired();

        // Relacionamentos
        builder.HasMany(u => u.Addresses)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SavedCards)
            .WithOne(sc => sc.User)
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Tokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Consents)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RevokedTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_users_email");

        builder.HasIndex(u => u.KeyCloakId)
            .IsUnique()
            .HasDatabaseName("IX_users_keycloak_id")
            .HasFilter("keycloak_id IS NOT NULL");

        builder.HasIndex(u => u.Cpf)
            .IsUnique()
            .HasDatabaseName("IX_users_cpf")
            .HasFilter("cpf IS NOT NULL");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_users_status");

        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("IX_users_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        // Configurações de consulta
        builder.HasQueryFilter(u => u.DeletedAt == null); // Soft delete global filter
    }
}