using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.User.Domain.ValueObjects;

namespace Bcommerce.User.Domain.Users;

public class UserProfile : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public Cpf? Cpf { get; private set; }
    
    // Preferences
    public string PreferredLanguage { get; private set; } = "pt-BR";
    public string PreferredCurrency { get; private set; } = "BRL";
    public bool NewsletterSubscribed { get; private set; }
    
    // Marketing
    public DateTime? AcceptedTermsAt { get; private set; }
    public DateTime? AcceptedPrivacyAt { get; private set; }

    public UserProfile(Guid userId, string firstName, string lastName, string displayName)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
    }

    // Required for EF Core
    protected UserProfile() { }

    public void UpdateProfile(string displayName, string? avatarUrl, DateTime? birthDate, string? gender)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        BirthDate = birthDate;
        Gender = gender;
    }

    public void SetCpf(Cpf cpf)
    {
        Cpf = cpf;
    }

    public void SetPreferences(string language, string currency, bool newsletter)
    {
        PreferredLanguage = language;
        PreferredCurrency = currency;
        NewsletterSubscribed = newsletter;
    }
}
