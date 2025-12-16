using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.ValueObjects;

public class Dimensions : ValueObject
{
    public decimal Height { get; private set; }
    public decimal Width { get; private set; }
    public decimal Length { get; private set; }

    public Dimensions(decimal height, decimal width, decimal length)
    {
        if (height <= 0 || width <= 0 || length <= 0)
        {
            throw new ArgumentException("As dimensões devem ser maiores que zero.");
        }

        Height = height;
        Width = width;
        Length = length;
    }

    public string FormattedDescription()
    {
        return $"{Height:N2}cm x {Width:N2}cm x {Length:N2}cm";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Height;
        yield return Width;
        yield return Length;
    }
}
