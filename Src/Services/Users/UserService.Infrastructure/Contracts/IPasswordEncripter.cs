namespace UserService.Infrastructure.Contracts;

public interface IPasswordEncripter
{
    string Encrypt(string password);

    bool Verify(string password, string passwordHash);
}