using Bielu.Ef.Examples.Blog.Configurations;
using Bielu.Ef.Examples.Blog.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bielu.Ef.Examples.Blog;

/// <summary>
/// BlogDbContext is responsible for migrating blog-related tables:
///   - BlogPosts
///   - BlogPostVersions
///
/// It references the Users table (owned by AuthDbContext) but explicitly EXCLUDES it
/// from migrations using <c>ExcludeFromMigrations()</c>. This means EF Core is aware of
/// the User entity for relationship/query purposes, but will NOT attempt to create or
/// modify the Users table when running Blog migrations.
///
/// At runtime, the Users table must already exist (created by AuthDbContext migrations).
/// </summary>
public class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogPostVersion> BlogPostVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlogPostConfiguration());
        modelBuilder.ApplyConfiguration(new BlogPostVersionConfiguration());

        // Include the User entity so EF Core can validate the FK relationship,
        // but exclude it from this context's migrations — AuthDbContext owns it.
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", t => t.ExcludeFromMigrations());
        });
    }
}
