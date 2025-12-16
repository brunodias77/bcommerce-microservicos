using Bcommerce.Catalog.Domain.Categories;
using Bcommerce.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.Catalog.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<StockReservation> StockReservations { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        
        // Ensure Npgsql extension is registered if needed for uuid generation
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm"); 
        

    }
}
