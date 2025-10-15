using System.Text.RegularExpressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;

namespace UserService.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email não pode ser vazio", nameof(email));

        if (!IsValid(email))
            throw new ArgumentException("Email inválido", nameof(email));

        return new Email(email.ToLowerInvariant());
    }

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        return emailRegex.IsMatch(email);
    }

    public override ValidationHandler Validate()
    {
        var validation = new ValidationHandler();
        
        if (string.IsNullOrWhiteSpace(Value))
        {
            validation.Add("EMAIL_REQUIRED", "Email é obrigatório");
        }
        else if (!IsValid(Value))
        {
            validation.Add("EMAIL_INVALID", "Email inválido");
        }

        return validation;
    }

    public string GetDomain()
    {
        if (string.IsNullOrWhiteSpace(Value) || !Value.Contains('@'))
            return string.Empty;

        return Value.Split('@')[1];
    }

    public override string ToString() => Value;

    // Implicit conversion from string
    public static implicit operator string(Email email) => email?.Value;
    
    // Explicit conversion to Email
    public static explicit operator Email(string email) => Create(email);
}