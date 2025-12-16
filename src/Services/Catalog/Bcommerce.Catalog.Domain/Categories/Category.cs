using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.Catalog.Domain.Categories;

public class Category : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation (optional in Domain, but useful)
    // public virtual Category? Parent { get; private set; }
    // public virtual ICollection<Category> SubCategories { get; private set; }

    public Category(string name, string slug, string? description = null, Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug; // Logic to generate slug should probably be in a service or use the DB function? 
                     // For domain, we usually accept it or generate simple regex. 
                     // The requirement says "generate classes", assuming basic constructor validation.
        Description = description;
        ParentId = parentId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    protected Category() { }

    public void Update(string name, string slug, string? description)
    {
        Name = name;
        Slug = slug;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id) throw new InvalidOperationException("Uma categoria não pode ser pai de si mesma.");
        ParentId = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
