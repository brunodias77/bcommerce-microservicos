using Bcommerce.BuildingBlocks.Core.Domain;
using Bcommerce.Catalog.Domain.Products.Events;
using Bcommerce.Catalog.Domain.ValueObjects;

namespace Bcommerce.Catalog.Domain.Products;

public enum ProductStatus { Draft, Active, Inactive, OutOfStock, Discontinued }

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Sku Sku { get; private set; }
    public string Slug { get; private set; }
    public Money Price { get; private set; }
    public int Stock { get; private set; }
    public int ReservedStock { get; private set; }
    public Guid? CategoryId { get; private set; }
    public ProductStatus Status { get; private set; }
    public Dimensions? Dimensions { get; private set; }

    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private readonly List<ProductReview> _reviews = new();
    public IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();

    public Product(string name, string description, string sku, Money price, Guid? categoryId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Sku = new Sku(sku);
        Slug = sku.ToLower(); // Simplification
        Price = price;
        CategoryId = categoryId;
        Status = ProductStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ProductCreatedEvent(Id, Name, Sku.Value));
    }

    protected Product() { }

    public void AddStock(int quantity)
    {
        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockUpdatedEvent(Id, Stock, ReservedStock));
    }
    
    public void RemoveStock(int quantity)
    {
        if (Stock < quantity) throw new InvalidOperationException("Estoque insuficiente.");
        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockUpdatedEvent(Id, Stock, ReservedStock));
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = Price.Amount;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PriceChangedEvent(Id, newPrice.Amount, oldPrice));
    }

    public void AddImage(string url, bool isPrimary = false)
    {
        _images.Add(new ProductImage(Id, url, Name, isPrimary));
    }
}
