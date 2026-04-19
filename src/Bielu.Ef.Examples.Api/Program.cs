using Bielu.Ef.Examples.Auth;
using Bielu.Ef.Examples.Blog;
using Bielu.Ef.Examples.Blog.Entities;
using Bielu.Ef.Examples.Profile;
using Bielu.Ef.Examples.Profile.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ------------------------------------------------------------------------------------
// Multiple DbContext Registration
//
// All three contexts share the SAME SQLite database (app.db) but manage different
// tables within it. In a production SQL Server setup you might use different schemas
// (e.g. [auth].[Users], [blog].[BlogPosts]) or even different databases — the migration
// ownership pattern works the same way.
//
// Key principle — migration ownership:
//   - AuthDbContext   owns & migrates: Users, Roles, UserRoles
//   - BlogDbContext   owns & migrates: BlogPosts, BlogPostVersions
//                    (references Users but excludes it from its migrations)
//   - ProfileDbContext owns & migrates: UserProfiles
//                    (references Users but excludes it from its migrations)
//
// The order of applying migrations matters: AuthDbContext must run first because
// the Users table needs to exist before Blog/Profile FKs reference it.
// ------------------------------------------------------------------------------------

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ProfileDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ------------------------------------------------------------------------------------
// Apply migrations and seed data on startup
// ------------------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // 1. AuthDbContext migrations first — creates Users, Roles, UserRoles
    var authDb = services.GetRequiredService<AuthDbContext>();
    authDb.Database.Migrate();

    // 2. BlogDbContext migrations — creates BlogPosts, BlogPostVersions
    //    Users table already exists from step 1; ExcludeFromMigrations() ensures
    //    BlogDbContext does not attempt to recreate it.
    var blogDb = services.GetRequiredService<BlogDbContext>();
    blogDb.Database.Migrate();

    // 3. ProfileDbContext migrations — creates UserProfiles
    //    Same pattern as BlogDbContext.
    var profileDb = services.GetRequiredService<ProfileDbContext>();
    profileDb.Database.Migrate();

    // Seed some demo data if the database is empty
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

        authDb.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = 1 }); // Admin role
        authDb.SaveChanges();

        // Seed blog post (uses same user ID across contexts via shared FK)
        var post = new BlogPost
        {
            Title = "Welcome to the Blog",
            Slug = "welcome-to-the-blog",
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
            Content = "This is the first version of the welcome post.",
            Summary = "Welcome post initial version",
            CreatedByUserId = adminUser.Id
        });
        blogDb.SaveChanges();

        // Seed user profile
        profileDb.UserProfiles.Add(new UserProfile
        {
            UserId = adminUser.Id,
            DisplayName = "Administrator",
            Bio = "The site administrator.",
            Location = "Poland"
        });
        profileDb.SaveChanges();
    }
}

// ------------------------------------------------------------------------------------
// Minimal API endpoints demonstrating cross-context queries
// ------------------------------------------------------------------------------------

// Auth endpoints
app.MapGet("/users", async (AuthDbContext db) =>
    await db.Users.Select(u => new { u.Id, u.Username, u.Email, u.IsActive }).ToListAsync())
    .WithName("GetUsers");

app.MapGet("/roles", async (AuthDbContext db) =>
    await db.Roles.Select(r => new { r.Id, r.Name, r.Description }).ToListAsync())
    .WithName("GetRoles");

// Blog endpoints
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

// Profile endpoints
app.MapGet("/profiles", async (ProfileDbContext db) =>
    await db.UserProfiles
        .Select(p => new { p.Id, p.UserId, p.DisplayName, p.Bio, p.Location })
        .ToListAsync())
    .WithName("GetProfiles");

// Cross-context example: enrich blog posts with user info from AuthDbContext
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

