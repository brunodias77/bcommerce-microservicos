using Bcommerce.Catalog.Domain.Products;

namespace Bcommerce.Catalog.Domain.Repositories;

public interface IStockRepository
{
    // Usually Stock is managed via Product Aggregate, but if complex movements are needed separately:
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default);
    Task RemoveReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockReservation>> GetActiveReservationsAsync(Guid productId, CancellationToken cancellationToken = default);
}
