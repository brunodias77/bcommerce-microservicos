using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.User.Domain.Users.Events;
using Microsoft.AspNetCore.Identity;

namespace Bcommerce.User.Domain.Users;

public class ApplicationUser : IdentityUser<Guid>, IAggregateRoot
{
    // Domain Events Implementation (since we can't inherit from Entity<T>)
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // Additional Properties not in IdentityUser
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigations
    public virtual UserProfile? Profile { get; private set; }
    public virtual ICollection<Address> Addresses { get; private set; } = new List<Address>();
    public virtual ICollection<UserSession> Sessions { get; private set; } = new List<UserSession>();
    public virtual ICollection<UserNotification> Notifications { get; private set; } = new List<UserNotification>();

    public ApplicationUser(string userName, string email) : base(userName)
    {
        Id = Guid.NewGuid();
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    // Required for EF Core
    public ApplicationUser() { }

    public void SetProfile(UserProfile profile)
    {
        Profile = profile;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProfileUpdatedEvent(Id, profile.DisplayName, profile.AvatarUrl ?? string.Empty));
    }

    public void AddAddress(Address address)
    {
        Addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AddressAddedEvent(Id, address.Id, address.Street, address.Number ?? "", address.City, address.State, address.PostalCode.Code));
    }
}
