using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.ValueObjects;

public class Sku : ValueObject
{
    public string Value { get; private set; }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU não pode ser vazio.");
        }

        Value = value.ToUpper().Trim();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
