using Bielu.Ef.Examples.Profile.Configurations;
using Bielu.Ef.Examples.Profile.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bielu.Ef.Examples.Profile;

/// <summary>
/// ProfileDbContext is responsible for migrating profile-related tables:
///   - UserProfiles
///
/// It references the Users table (owned by AuthDbContext) but explicitly EXCLUDES it
/// from migrations using <c>ExcludeFromMigrations()</c>. This means EF Core is aware of
/// the User entity for relationship/query purposes, but will NOT attempt to create or
/// modify the Users table when running Profile migrations.
///
/// At runtime, the Users table must already exist (created by AuthDbContext migrations).
/// </summary>
public class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());

        // Include the User entity so EF Core can validate the FK relationship,
        // but exclude it from this context's migrations — AuthDbContext owns it.
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", t => t.ExcludeFromMigrations());
        });
    }
}
