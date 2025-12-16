using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.User.Domain.ValueObjects;

namespace Bcommerce.User.Domain.Users;

public class Address : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Label { get; private set; }
    public string RecipientName { get; private set; }
    public string Street { get; private set; }
    public string? Number { get; private set; }
    public string? Complement { get; private set; }
    public string? Neighborhood { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public PostalCode PostalCode { get; private set; }
    public string Country { get; private set; } = "BR";
    public bool IsDefault { get; private set; }
    public bool IsBillingAddress { get; private set; }

    public Address(Guid userId, string label, string recipientName, string street, string city, string state, PostalCode postalCode)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Label = label;
        RecipientName = recipientName;
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
    }

    // Required for EF Core
    protected Address() { }

    public void SetComplement(string? number, string? complement, string? neighborhood)
    {
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
    }

    public void SetAsDefault() => IsDefault = true;
    public void RemoveDefault() => IsDefault = false;
    public void SetAsBilling() => IsBillingAddress = true;
}
