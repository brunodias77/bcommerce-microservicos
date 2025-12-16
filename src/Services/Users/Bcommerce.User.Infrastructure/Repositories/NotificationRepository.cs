using Bcommerce.User.Domain.Repositories;
using Bcommerce.User.Domain.Users;
using Bcommerce.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.User.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly UserDbContext _context;

    public NotificationRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserNotification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .OrderByDescending(n => n.Id) // UUID not sortable by ID usually, but using CreatedAt is safer if available? 
            // Checking definition: UserNotification only has Entity<Guid> props. Assuming CreatedAt is needed or sort by something else.
            // Entity<T> has CreatedAt? Let me check domain... Yes, Entity<TId> has CreatedAt.
            // But EF Core might not translate property getter from base class unless mapped correctly?
            // Actually Entity<TId> has CreatedAt property.
            .OrderByDescending(n => n.CreatedAt) 
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        await _context.UserNotifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        _context.UserNotifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
