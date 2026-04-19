using Bielu.Ef.Examples.Auth;
using Bielu.Ef.Examples.Blog;
using Bielu.Ef.Examples.Blog.Entities;
using Bielu.Ef.Examples.Profile;
using Bielu.Ef.Examples.Profile.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// ------------------------------------------------------------------------------------
// Multiple DbContext Registration — Postgres edition
//
// All three contexts share the SAME PostgreSQL database (provisioned by the
// Aspire AppHost as the "biele-ef-examples-db" resource). Each context still
// owns its own subset of tables, just like the SQLite example, demonstrating
// that the multi-context migration-ownership pattern is provider-agnostic.
//
// Migration ownership:
//   - AuthDbContext   owns & migrates: Users, Roles, UserRoles
//   - BlogDbContext   owns & migrates: BlogPosts, BlogPostVersions
//                    (references Users but excludes it from its migrations)
//   - ProfileDbContext owns & migrates: UserProfiles
//                    (references Users but excludes it from its migrations)
//
// IMPORTANT — provider-specific migrations:
//   The class libraries (Auth/Blog/Profile) ship SQLite migrations. The Postgres
//   migrations for the very same DbContexts are stored in THIS project under
//   /Migrations/{Auth,Blog,Profile}. We tell EF Core where to find them via
//   `MigrationsAssembly(...)` so the same DbContext type can be used with both
//   providers without conflicting migration history.
// ------------------------------------------------------------------------------------

const string ConnectionName = "biele-ef-examples-db";
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;

builder.AddNpgsqlDbContext<AuthDbContext>(
    connectionName: ConnectionName,
    configureDbContextOptions: options =>
        options.UseNpgsql(npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

builder.AddNpgsqlDbContext<BlogDbContext>(
    connectionName: ConnectionName,
    configureDbContextOptions: options =>
        options.UseNpgsql(npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

builder.AddNpgsqlDbContext<ProfileDbContext>(
    connectionName: ConnectionName,
    configureDbContextOptions: options =>
        options.UseNpgsql(npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ------------------------------------------------------------------------------------
// Apply migrations and seed data on startup. Order matters: AuthDbContext first
// because BlogDbContext / ProfileDbContext have FKs to the Users table.
// ------------------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var authDb = services.GetRequiredService<AuthDbContext>();
    authDb.Database.Migrate();

    var blogDb = services.GetRequiredService<BlogDbContext>();
    blogDb.Database.Migrate();

    var profileDb = services.GetRequiredService<ProfileDbContext>();
    profileDb.Database.Migrate();

    if (!authDb.Users.Any())
    {
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hashed_password_here",
            IsActive = true
        };
        authDb.Users.Add(adminUser);
        authDb.SaveChanges();

        authDb.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = 1 });
        authDb.SaveChanges();

        var post = new BlogPost
        {
            Title = "Welcome to the Postgres Blog",
            Slug = "welcome-to-the-postgres-blog",
            AuthorId = adminUser.Id,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };
        blogDb.BlogPosts.Add(post);
        blogDb.SaveChanges();

        blogDb.BlogPostVersions.Add(new BlogPostVersion
        {
            BlogPostId = post.Id,
            VersionNumber = 1,
            Content = "This is the first version of the welcome post (Postgres edition).",
            Summary = "Welcome post initial version",
            CreatedByUserId = adminUser.Id
        });
        blogDb.SaveChanges();

        profileDb.UserProfiles.Add(new UserProfile
        {
            UserId = adminUser.Id,
            DisplayName = "Administrator",
            Bio = "The site administrator (Postgres).",
            Location = "Poland"
        });
        profileDb.SaveChanges();
    }
}

// ------------------------------------------------------------------------------------
// Minimal API endpoints — identical surface to the SQLite example, so callers
// can swap between the two with no contract changes.
// ------------------------------------------------------------------------------------

app.MapGet("/users", async (AuthDbContext db) =>
    await db.Users.Select(u => new { u.Id, u.Username, u.Email, u.IsActive }).ToListAsync())
    .WithName("GetUsers");

app.MapGet("/roles", async (AuthDbContext db) =>
    await db.Roles.Select(r => new { r.Id, r.Name, r.Description }).ToListAsync())
    .WithName("GetRoles");

app.MapGet("/blog/posts", async (BlogDbContext db) =>
    await db.BlogPosts
        .Include(p => p.Versions)
        .Select(p => new
        {
            p.Id,
            p.Title,
            p.Slug,
            p.AuthorId,
            p.IsPublished,
            p.PublishedAt,
            VersionCount = p.Versions.Count
        })
        .ToListAsync())
    .WithName("GetBlogPosts");

app.MapGet("/profiles", async (ProfileDbContext db) =>
    await db.UserProfiles
        .Select(p => new { p.Id, p.UserId, p.DisplayName, p.Bio, p.Location })
        .ToListAsync())
    .WithName("GetProfiles");

app.MapGet("/blog/posts/enriched", async (BlogDbContext blogDb, AuthDbContext authDb) =>
{
    var posts = await blogDb.BlogPosts.ToListAsync();
    var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
    var authors = await authDb.Users
        .Where(u => authorIds.Contains(u.Id))
        .ToDictionaryAsync(u => u.Id, u => u.Username);

    return posts.Select(p => new
    {
        p.Id,
        p.Title,
        p.Slug,
        AuthorUsername = authors.TryGetValue(p.AuthorId, out var name) ? name : "Unknown",
        p.IsPublished
    });
})
.WithName("GetEnrichedBlogPosts");

app.Run();

// Make the implicit Program class visible to the AppHost's source generator.
public partial class Program;
