using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Products;

public class StockReservation : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ReferenceType { get; private set; } // CART, ORDER
    public Guid ReferenceId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    public StockReservation(Guid productId, string referenceType, Guid referenceId, int quantity, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    protected StockReservation() { }

    public void Release() { ReleasedAt = DateTime.UtcNow; }
}
