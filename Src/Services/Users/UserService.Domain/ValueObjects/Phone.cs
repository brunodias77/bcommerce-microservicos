using System.Text.RegularExpressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;

namespace UserService.Domain.ValueObjects;

public class Phone : ValueObject
{
    public string Value { get; private set; }

    private Phone(string value)
    {
        Value = value;
    }

    public static Phone Create(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Telefone não pode ser vazio", nameof(phone));

        var cleanPhone = Regex.Replace(phone, @"[^\d]", "");
        
        if (!IsValid(cleanPhone))
            throw new ArgumentException("Telefone inválido", nameof(phone));

        return new Phone(cleanPhone);
    }

    public static bool IsValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var cleanPhone = Regex.Replace(phone, @"[^\d]", "");
        
        // Telefone brasileiro: 10 ou 11 dígitos (com ou sem 9 no celular)
        return cleanPhone.Length == 10 || cleanPhone.Length == 11;
    }

    public override ValidationHandler Validate()
    {
        var validation = new ValidationHandler();
        
        if (string.IsNullOrWhiteSpace(Value))
        {
            validation.Add("PHONE_REQUIRED", "Telefone é obrigatório");
        }
        else if (!IsValid(Value))
        {
            validation.Add("PHONE_INVALID", "Telefone inválido");
        }

        return validation;
    }

    public string GetFormattedValue()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return Value;

        if (Value.Length == 10)
            return $"({Value.Substring(0, 2)}) {Value.Substring(2, 4)}-{Value.Substring(6, 4)}";
        
        if (Value.Length == 11)
            return $"({Value.Substring(0, 2)}) {Value.Substring(2, 5)}-{Value.Substring(7, 4)}";

        return Value;
    }

    public override string ToString() => Value;

    // Implicit conversion from string
    public static implicit operator string(Phone phone) => phone?.Value;
    
    // Explicit conversion to Phone
    public static explicit operator Phone(string phone) => Create(phone);
}