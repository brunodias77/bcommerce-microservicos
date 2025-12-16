using Bcommerce.User.Domain.Users;

namespace Bcommerce.User.Domain.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<UserNotification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken = default);
}
