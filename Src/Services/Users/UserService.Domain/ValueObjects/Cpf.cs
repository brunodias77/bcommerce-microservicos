using System.Text.RegularExpressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Validations;

namespace UserService.Domain.ValueObjects;

public class Cpf : ValueObject
{
    public string Value { get; private set; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new ArgumentException("CPF não pode ser vazio", nameof(cpf));

        var cleanCpf = Regex.Replace(cpf, @"[^\d]", "");
        
        if (!IsValid(cleanCpf))
            throw new ArgumentException("CPF inválido", nameof(cpf));

        return new Cpf(cleanCpf);
    }

    public static bool IsValid(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        var cleanCpf = Regex.Replace(cpf, @"[^\d]", "");

        if (cleanCpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cleanCpf.All(c => c == cleanCpf[0]))
            return false;

        // Calcula o primeiro dígito verificador
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += int.Parse(cleanCpf[i].ToString()) * (10 - i);
        }
        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cleanCpf[9].ToString()) != firstDigit)
            return false;

        // Calcula o segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += int.Parse(cleanCpf[i].ToString()) * (11 - i);
        }
        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cleanCpf[10].ToString()) == secondDigit;
    }

    public override ValidationHandler Validate()
    {
        var validation = new ValidationHandler();
        
        if (string.IsNullOrWhiteSpace(Value))
        {
            validation.Add("CPF_REQUIRED", "CPF é obrigatório");
        }
        else if (!IsValid(Value))
        {
            validation.Add("CPF_INVALID", "CPF inválido");
        }

        return validation;
    }

    public string GetFormattedValue()
    {
        if (string.IsNullOrWhiteSpace(Value) || Value.Length != 11)
            return Value;

        return $"{Value.Substring(0, 3)}.{Value.Substring(3, 3)}.{Value.Substring(6, 3)}-{Value.Substring(9, 2)}";
    }

    public override string ToString() => Value;

    // Implicit conversion from string
    public static implicit operator string(Cpf cpf) => cpf?.Value;
    
    // Explicit conversion to Cpf
    public static explicit operator Cpf(string cpf) => Create(cpf);
}