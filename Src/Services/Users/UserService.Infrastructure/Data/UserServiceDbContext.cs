using Microsoft.EntityFrameworkCore;
using UserService.Domain.Aggregates;
using UserService.Domain.Entities;
using UserService.Domain.ValueObjects;
using UserService.Infrastructure.Data.Configurations;

namespace UserService.Infrastructure.Data;

public class UserServiceDbContext : DbContext
{
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options) : base(options)
    {
    }

    // DbSets para as entidades do domínio
    public DbSet<User> Users { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<UserConsent> UserConsents { get; set; }
    public DbSet<SavedCard> SavedCards { get; set; }
    public DbSet<RevokedJwtToken> RevokedJwtTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         base.OnModelCreating(modelBuilder);

        // Aplicar configurações das entidades
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserAddressConfiguration());
        modelBuilder.ApplyConfiguration(new UserTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserConsentConfiguration());
        modelBuilder.ApplyConfiguration(new SavedCardConfiguration());
        modelBuilder.ApplyConfiguration(new RevokedJwtTokenConfiguration());

        // Configurações globais
        ConfigureValueObjects(modelBuilder);
        ConfigureEnums(modelBuilder);
    }

    /// <summary>
    /// Configura os Value Objects para serem armazenados como propriedades simples
    /// </summary>
    private void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Configurar Email como string
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        // Configurar Phone como string
        modelBuilder.Entity<User>()
            .Property(u => u.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? Phone.Create(value) : null)
            .HasColumnName("phone")
            .HasMaxLength(20);

        // Configurar Cpf como string
        modelBuilder.Entity<User>()
            .Property(u => u.Cpf)
            .HasConversion(
                cpf => cpf != null ? cpf.Value : null,
                value => value != null ? Cpf.Create(value) : null)
            .HasColumnName("cpf")
            .HasMaxLength(11);

        // SavedCard não possui CardInfo como propriedade - removido
    }

    /// <summary>
    /// Configura os Enums para serem armazenados como strings
    /// </summary>
    private void ConfigureEnums(ModelBuilder modelBuilder)
    {
        // UserStatus como string
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        // UserRole como string
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>()
            .HasColumnName("role")
            .HasMaxLength(20);

        // AddressType como string
        modelBuilder.Entity<UserAddress>()
            .Property(ua => ua.Type)
            .HasConversion<string>()
            .HasColumnName("type")
            .HasMaxLength(20);

        // TokenType como string
        modelBuilder.Entity<UserToken>()
            .Property(ut => ut.TokenType)
            .HasConversion<string>()
            .HasColumnName("token_type")
            .HasMaxLength(50);

        // ConsentType como string
        modelBuilder.Entity<UserConsent>()
            .Property(uc => uc.Type)
            .HasConversion<string>()
            .HasColumnName("type")
            .HasMaxLength(50);

        // CardBrand como string
        modelBuilder.Entity<SavedCard>()
            .Property(sc => sc.Brand)
            .HasConversion<string>()
            .HasColumnName("brand")
            .HasMaxLength(20);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Configuração de fallback - normalmente a configuração vem do DI
            optionsBuilder.UseNpgsql();
        }

        // Configurações de desenvolvimento
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }
}