using Microsoft.EntityFrameworkCore;

namespace UserService.Infrastructure.Data;

public class UserServiceDbContext : DbContext
{
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options) : base(options)
    {
    }
}