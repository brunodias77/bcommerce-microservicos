using Bcommerce.Catalog.Domain.Products;
using Bcommerce.Catalog.Domain.Repositories;
using Bcommerce.Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.Catalog.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly CatalogDbContext _context;

    public StockRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _context.StockMovements.AddAsync(movement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.StockReservations.AddAsync(reservation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        _context.StockReservations.Remove(reservation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockReservation>> GetActiveReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .Where(r => r.ProductId == productId && r.ReleasedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
