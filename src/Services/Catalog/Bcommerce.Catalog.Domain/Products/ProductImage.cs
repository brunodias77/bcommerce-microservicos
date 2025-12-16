using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Products;

public class ProductImage : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; }
    public string? AltText { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    public ProductImage(Guid productId, string url, string? altText, bool isPrimary)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Url = url;
        AltText = altText;
        IsPrimary = isPrimary;
        CreatedAt = DateTime.UtcNow;
    }

    protected ProductImage() { }

    public void SetPrimary(bool primary) => IsPrimary = primary;
}
