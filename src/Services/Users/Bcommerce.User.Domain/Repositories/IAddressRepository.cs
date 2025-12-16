using Bcommerce.User.Domain.Users;

namespace Bcommerce.User.Domain.Repositories;

public interface IAddressRepository
{
    Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Address>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Address address, CancellationToken cancellationToken = default);
    Task DeleteAsync(Address address, CancellationToken cancellationToken = default);
}
