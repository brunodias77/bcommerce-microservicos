// src/BuildingBlocks/Common/Common.Domain/ValueObjects/Email.cs
namespace Common.Domain.ValueObjects;

using System.Text.RegularExpressions;

/// <summary>
/// Value Object para endereços de e-mail
/// </summary>
public partial class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        value = value.Trim().ToLower();

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value;
    }

    private static bool IsValidEmail(string email)
    {
        return EmailRegex().IsMatch(email);
    }

    public string GetDomain()
    {
        return Value.Split('@')[1];
    }

    public string GetLocalPart()
    {
        return Value.Split('@')[0];
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}