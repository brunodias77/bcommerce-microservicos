namespace Common.Application.Interfaces;

/// <summary>
/// Interface para obter informações do usuário atual
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }
}