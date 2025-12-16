using Bcommerce.User.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bcommerce.User.Infrastructure.Data.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.AvatarUrl);
        builder.Property(p => p.BirthDate).HasColumnType("date");
        builder.Property(p => p.Gender).HasMaxLength(20);

        // Value Object: CPF
        builder.OwnsOne(p => p.Cpf, cpf =>
        {
            cpf.Property(c => c.Number)
               .HasColumnName("cpf")
               .HasMaxLength(14);
            
            cpf.HasIndex(c => c.Number).IsUnique();
        });

        // Preferences
        builder.Property(p => p.PreferredLanguage).HasMaxLength(5).HasDefaultValue("pt-BR");
        builder.Property(p => p.PreferredCurrency).HasMaxLength(3).HasDefaultValue("BRL");
        builder.Property(p => p.NewsletterSubscribed).HasDefaultValue(false);

        // Marketing
        builder.Property(p => p.AcceptedTermsAt);
        builder.Property(p => p.AcceptedPrivacyAt);

        // Timestamps
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
