using Bcommerce.User.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bcommerce.User.Infrastructure.Data;

public class UserDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply configurations from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);

        // Customize Identity Table Names (Optional, but often cleaner in Postgres)
        // builder.Entity<ApplicationUser>().ToTable("users");
        // builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        // But keeping defaults AspNetUsers is safer for standard Identity compatibility
    }
}
