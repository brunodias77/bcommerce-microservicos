using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Products;

public enum StockMovementType { In, Out, Adjustment, Reserve, Release }

public class StockMovement : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public int StockBefore { get; private set; }
    public int StockAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public StockMovement(Guid productId, StockMovementType movementType, int quantity, int stockBefore, int stockAfter, string? referenceType, Guid? referenceId)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        MovementType = movementType;
        Quantity = quantity;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        CreatedAt = DateTime.UtcNow;
    }

    protected StockMovement() { }
}
