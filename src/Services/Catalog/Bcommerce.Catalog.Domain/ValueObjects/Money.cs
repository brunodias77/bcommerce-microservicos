using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
        {
            throw new ArgumentException("O valor monetário não pode ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("A moeda é obrigatória.");
        }

        Amount = amount;
        Currency = currency.ToUpper();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString()
    {
        return $"{Currency} {Amount:N2}";
    }
}
