using Bcommerce.User.Domain.Repositories;
using Bcommerce.User.Domain.Users;
using Bcommerce.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.User.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly UserDbContext _context;

    public AddressRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<Address?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Address>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Address address, CancellationToken cancellationToken = default)
    {
        await _context.Addresses.AddAsync(address, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Address address, CancellationToken cancellationToken = default)
    {
        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
