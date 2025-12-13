// src/BuildingBlocks/Common/Common.Domain/ValueObjects/Address.cs

namespace Common.Domain.ValueObjects;


/// <summary>
/// Value Object para endereços brasileiros
/// </summary>
public class Address : ValueObject
{
    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string Neighborhood { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(
        string street,
        string number,
        string neighborhood,
        string city,
        string state,
        string postalCode,
        string? complement = null,
        string country = "BR")
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty", nameof(street));

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Number cannot be empty", nameof(number));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (string.IsNullOrWhiteSpace(state) || state.Length != 2)
            throw new ArgumentException("State must be 2 characters", nameof(state));

        if (!IsValidPostalCode(postalCode))
            throw new ArgumentException("Invalid postal code format", nameof(postalCode));

        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state.ToUpper();
        PostalCode = FormatPostalCode(postalCode);
        Country = country.ToUpper();
    }

    private static bool IsValidPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return false;

        var cleaned = postalCode.Replace("-", "").Replace(".", "");
        return cleaned.Length == 8 && cleaned.All(char.IsDigit);
    }

    private static string FormatPostalCode(string postalCode)
    {
        var cleaned = postalCode.Replace("-", "").Replace(".", "");
        return $"{cleaned.Substring(0, 5)}-{cleaned.Substring(5)}";
    }

    public string GetFullAddress()
    {
        var complement = string.IsNullOrWhiteSpace(Complement) ? "" : $", {Complement}";
        return $"{Street}, {Number}{complement}, {Neighborhood}, {City}/{State}, {PostalCode}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return Neighborhood;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => GetFullAddress();
}