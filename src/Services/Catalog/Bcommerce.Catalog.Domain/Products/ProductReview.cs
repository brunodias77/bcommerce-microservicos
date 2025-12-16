using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Products;

public class ProductReview : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public bool IsApproved { get; private set; }

    public ProductReview(Guid productId, Guid userId, int rating, string? title, string? comment)
    {
        if (rating < 1 || rating > 5) throw new ArgumentOutOfRangeException(nameof(rating), "A avaliação deve ser entre 1 e 5.");

        Id = Guid.NewGuid();
        ProductId = productId;
        UserId = userId;
        Rating = rating;
        Title = title;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    protected ProductReview() { }

    public void Approve() => IsApproved = true;
    public void VerifyPurchase() => IsVerifiedPurchase = true;
}
