using Bcommerce.BuildingBlocks.Core.Domain;

namespace Bcommerce.User.Domain.Users;

public class UserSession : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public string? DeviceId { get; private set; }
    public string? DeviceName { get; private set; }
    public string? DeviceType { get; private set; }
    public string? IpAddress { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    public UserSession(Guid userId, string refreshTokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        LastActivityAt = DateTime.UtcNow;
    }

    // Required for EF Core
    protected UserSession() { }

    public void SetDeviceInfo(string deviceId, string deviceName, string deviceType, string ipAddress)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceType = deviceType;
        IpAddress = ipAddress;
    }

    public void Revoke(string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
    }
}
