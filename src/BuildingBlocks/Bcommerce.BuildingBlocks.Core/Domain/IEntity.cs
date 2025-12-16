namespace Bcommerce.BuildingBlocks.Core.Domain;

public interface IEntity
{
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
