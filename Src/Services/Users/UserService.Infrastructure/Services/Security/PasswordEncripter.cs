using UserService.Infrastructure.Contracts;

namespace UserService.Infrastructure.Services.Security;

public class PasswordEncripter : IPasswordEncripter
{
    public string Encrypt(string password)
    {
        throw new NotImplementedException();
    }

    public bool Verify(string password, string passwordHash)
    {
        throw new NotImplementedException();
    }
}