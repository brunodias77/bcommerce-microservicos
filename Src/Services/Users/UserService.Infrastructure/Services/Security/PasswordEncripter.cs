using UserService.Infrastructure.Contracts;

namespace UserService.Infrastructure.Services.Security;

public class PasswordEncripter : IPasswordEncripter
{
    public string Encrypt(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}