using Bielu.Ef.Examples.Auth.Configurations;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bielu.Ef.Examples.Auth;

/// <summary>
/// AuthDbContext is the OWNER of auth-related tables:
///   - Users
///   - Roles
///   - UserRoles
///
/// This context is the ONLY one responsible for creating and migrating these tables.
/// BlogDbContext and ProfileDbContext will reference the Users table but will
/// exclude it from their own migrations using ToTable(..., t => t.ExcludeFromMigrations()).
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
    }
}
