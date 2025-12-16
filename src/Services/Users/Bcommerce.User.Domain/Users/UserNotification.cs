using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.User.Domain.Users;

public class UserNotification : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string NotificationType { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public UserNotification(Guid userId, string title, string message, string notificationType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        Message = message;
        NotificationType = notificationType;
    }

    // Required for EF Core
    protected UserNotification() { }

    public void MarkAsRead()
    {
        ReadAt = DateTime.UtcNow;
    }
}
