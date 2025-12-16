using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.User.Domain.ValueObjects;

public class Cpf : ValueObject
{
    public string Number { get; private set; }

    public Cpf(string number)
    {
        if (!Validate(number))
        {
            throw new ArgumentException("CPF inválido.");
        }

        Number = Clean(number);
    }

    public static string Clean(string cpf)
    {
        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public static bool Validate(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        var tempCpf = Clean(cpf);

        if (tempCpf.Length != 11)
            return false;

        var allDigitsEqual = true;
        for (var i = 1; i < tempCpf.Length; i++)
        {
            if (tempCpf[i] != tempCpf[0])
            {
                allDigitsEqual = false;
                break;
            }
        }

        if (allDigitsEqual)
            return false;

        var multiplier1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempPdf1 = tempCpf.Substring(0, 9);
        var sum = 0;

        for (var i = 0; i < 9; i++)
            sum += int.Parse(tempPdf1[i].ToString()) * multiplier1[i];

        var remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        var digit = remainder.ToString();
        var tempCpf2 = tempPdf1 + digit;
        sum = 0;

        for (var i = 0; i < 10; i++)
            sum += int.Parse(tempCpf2[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        digit += remainder.ToString();

        return tempCpf.EndsWith(digit);
    }

    public override string ToString()
    {
        return Number;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Number;
    }
}
