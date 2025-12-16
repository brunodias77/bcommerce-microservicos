namespace Bcommerce.BuildingBlocks.Infrastructure.Data;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
