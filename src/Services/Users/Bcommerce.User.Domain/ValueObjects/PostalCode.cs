using Bcommerce.BuildingBlocks.Core.Domain;
using System.Text.RegularExpressions;

namespace Bcommerce.User.Domain.ValueObjects;

public class PostalCode : ValueObject
{
    public string Code { get; private set; }

    public PostalCode(string code)
    {
        if (!Validate(code))
        {
            throw new ArgumentException("CEP inválido.");
        }

        Code = Clean(code);
    }

    public static string Clean(string code)
    {
        return new string(code.Where(char.IsDigit).ToArray());
    }

    public static bool Validate(string code)
    {
         if (string.IsNullOrWhiteSpace(code))
            return false;

        var cleanCode = Clean(code);
        if (cleanCode.Length != 8)
            return false;

        return Regex.IsMatch(code, @"^\d{5}-?\d{3}$");
    }

    public override string ToString()
    {
        return Code;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }
}
