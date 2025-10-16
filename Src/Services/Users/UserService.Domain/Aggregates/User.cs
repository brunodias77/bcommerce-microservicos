using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Aggregates;

[Table("users")]
public class User : AggregateRoot
{
        [Key]
    [Column("user_id")]
    public Guid UserId { get; private set; } = Guid.NewGuid();
    
    [Column("keycloak_id")]
    public Guid? KeyCloakId { get; private set; }
    
    [Required]
    [MaxLength(100)]
    [Column("first_name")]
    public string FirstName { get; private set; }
    
    [Required]
    [MaxLength(155)]
    [Column("last_name")]
    public string LastName { get; private set; }
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    [Column("email")]
    public Email Email { get; private set; }
    
    [Column("email_verified_at")]
    public DateTime?  EmailVerifiedAt { get; private set; }
    
    [MaxLength(20)]
    [Column("phone")]
    public Phone? Phone { get; set; }

    [MaxLength(255)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [StringLength(11, MinimumLength = 11)]
    [Column("cpf")]
    public Cpf? Cpf { get; set; }

    [Column("date_of_birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Column("newsletter_opt_in")]
    public bool NewsletterOptIn { get; set; } = false;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public UserStatus Status { get; set; } = UserStatus.Ativo;

    [Required]
    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Customer;

    [Column("failed_login_attempts")]
    public short FailedLoginAttempts { get; set; } = 0;

    [Column("account_locked_until")]
    public DateTime? AccountLockedUntil { get; set; }



    // Navigation properties
    public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public virtual ICollection<SavedCard> SavedCards { get; set; } = new List<SavedCard>();
    public virtual ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();
    public virtual ICollection<UserConsent> Consents { get; set; } = new List<UserConsent>();
    public virtual ICollection<RevokedJwtToken> RevokedTokens { get; set; } = new List<RevokedJwtToken>();


    // Private constructor for EF Core
    private User() { }

    // Factory method to create a new user
    public static User Create(
        string firstName,
        string lastName,
        Email email,
        UserRole role = UserRole.Customer,
        Guid? keyCloakId = null)
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            KeyCloakId = keyCloakId,
            Status = UserStatus.Ativo,
            NewsletterOptIn = false,
            FailedLoginAttempts = 0
        };

        return user;
    }

    // Business methods
    public void UpdateProfile(string firstName, string lastName, Phone? phone = null, DateTime? dateOfBirth = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
    }

    public void VerifyEmail()
    {
        EmailVerifiedAt = DateTime.UtcNow;
    }

    public void LockAccount(DateTime lockUntil)
    {
        AccountLockedUntil = lockUntil;
        Status = UserStatus.Inativo;
    }

    public void UnlockAccount()
    {
        AccountLockedUntil = null;
        FailedLoginAttempts = 0;
        Status = UserStatus.Ativo;
    }

    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        Status = UserStatus.Inativo;
    }
    
    public override ValidationHandler Validate()
    {
        var validationHandler = new ValidationHandler();

        // Validação do nome
        if (string.IsNullOrWhiteSpace(FirstName))
            validationHandler.Add("FirstName", "O nome é obrigatório");
        else if (FirstName.Length > 100)
            validationHandler.Add("FirstName", "O nome deve ter no máximo 100 caracteres");

        // Validação do sobrenome
        if (string.IsNullOrWhiteSpace(LastName))
            validationHandler.Add("LastName", "O sobrenome é obrigatório");
        else if (LastName.Length > 155)
            validationHandler.Add("LastName", "O sobrenome deve ter no máximo 155 caracteres");

        // Validação do email
        if (Email == null)
            validationHandler.Add("Email", "O email é obrigatório");

        // Validação da data de nascimento
        if (DateOfBirth.HasValue && DateOfBirth.Value > DateTime.Today)
            validationHandler.Add("DateOfBirth", "A data de nascimento não pode ser no futuro");

        // Validação de tentativas de login
        if (FailedLoginAttempts < 0)
            validationHandler.Add("FailedLoginAttempts", "O número de tentativas de login falhadas não pode ser negativo");

        return validationHandler;
    }
}