using BuildingBlocks.Data;
using UserService.Domain.Aggregates;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User entity, CancellationToken cancellationToken = default);
    void Update(User entity);
    void Delete(User entity);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
}