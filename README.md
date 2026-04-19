# Bielu.ef.examples

## Multiple DbContext Example — Shared Tables with Separate Migration Ownership

This repository demonstrates how to use **multiple EF Core DbContexts** in a single application where some contexts need to **share data** (e.g., reference a shared `Users` table) while each context remains **responsible only for its own migrations**.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Shared Database (app.db)              │
│                                                         │
│  ┌─── AuthDbContext ──────────────────────────────────┐ │
│  │  OWNS & MIGRATES:                                  │ │
│  │    ✓ Users        (auth-related fields)            │ │
│  │    ✓ Roles                                         │ │
│  │    ✓ UserRoles    (join table)                     │ │
│  └────────────────────────────────────────────────────┘ │
│                                                         │
│  ┌─── BlogDbContext ──────────────────────────────────┐ │
│  │  OWNS & MIGRATES:                                  │ │
│  │    ✓ BlogPosts      (AuthorId → Users.Id)          │ │
│  │    ✓ BlogPostVersions (CreatedByUserId → Users.Id) │ │
│  │  REFERENCES (ExcludeFromMigrations):               │ │
│  │    ◌ Users          (owned by AuthDbContext)       │ │
│  └────────────────────────────────────────────────────┘ │
│                                                         │
│  ┌─── ProfileDbContext ───────────────────────────────┐ │
│  │  OWNS & MIGRATES:                                  │ │
│  │    ✓ UserProfiles  (UserId → Users.Id)             │ │
│  │  REFERENCES (ExcludeFromMigrations):               │ │
│  │    ◌ Users          (owned by AuthDbContext)       │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

---

## Projects

| Project | Type | Responsibility |
|---------|------|----------------|
| `Bielu.Ef.Examples.Shared` | Class Library | Shared domain entity (`User`) referenced by multiple contexts |
| `Bielu.Ef.Examples.Auth` | Class Library | `AuthDbContext` — owns Users, Roles, UserRoles |
| `Bielu.Ef.Examples.Blog` | Class Library | `BlogDbContext` — owns BlogPosts, BlogPostVersions |
| `Bielu.Ef.Examples.Profile` | Class Library | `ProfileDbContext` — owns UserProfiles |
| `Bielu.Ef.Examples.Api` | ASP.NET Core Web API | Demo API wiring all contexts; applies migrations on startup |

---

## Key EF Core Pattern: `ExcludeFromMigrations()`

The central technique used here is `ToTable("Users", t => t.ExcludeFromMigrations())`.

When `BlogDbContext` or `ProfileDbContext` needs to express a foreign-key relationship to the `Users` table, they include the `User` entity in their model configuration **but mark it as excluded from migrations**:

```csharp
// In BlogDbContext.OnModelCreating:
modelBuilder.Entity<User>(entity =>
{
    entity.ToTable("Users", t => t.ExcludeFromMigrations());
});
```

This means:
- EF Core **knows about** the `User` entity (validates FK relationships, enables queries)
- EF Core **will NOT** try to `CREATE TABLE "Users"` when running `BlogDbContext` migrations
- The `Users` table is only created/managed by `AuthDbContext` migrations

---

## Running the Example

### Prerequisites

- .NET 9 SDK
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### Run

```bash
cd src/Bielu.Ef.Examples.Api
dotnet run
```

On startup the application will:
1. Apply `AuthDbContext` migrations → creates `Users`, `Roles`, `UserRoles`
2. Apply `BlogDbContext` migrations → creates `BlogPosts`, `BlogPostVersions` (with FK to `Users`)
3. Apply `ProfileDbContext` migrations → creates `UserProfiles` (with FK to `Users`)
4. Seed demo data (admin user, roles, sample blog post, user profile)

### Endpoints

| Method | Endpoint | Context Used | Description |
|--------|----------|-------------|-------------|
| GET | `/users` | `AuthDbContext` | List all users |
| GET | `/roles` | `AuthDbContext` | List all roles |
| GET | `/blog/posts` | `BlogDbContext` | List blog posts with version counts |
| GET | `/profiles` | `ProfileDbContext` | List user profiles |
| GET | `/blog/posts/enriched` | `BlogDbContext` + `AuthDbContext` | Blog posts enriched with author username (cross-context query at the application level) |

### Adding Migrations

Each context has its own migrations folder. To add a new migration for a specific context:

```bash
# AuthDbContext
dotnet ef migrations add <MigrationName> \
  --project src/Bielu.Ef.Examples.Auth \
  --startup-project src/Bielu.Ef.Examples.Api \
  --context AuthDbContext \
  --output-dir Migrations

# BlogDbContext
dotnet ef migrations add <MigrationName> \
  --project src/Bielu.Ef.Examples.Blog \
  --startup-project src/Bielu.Ef.Examples.Api \
  --context BlogDbContext \
  --output-dir Migrations

# ProfileDbContext
dotnet ef migrations add <MigrationName> \
  --project src/Bielu.Ef.Examples.Profile \
  --startup-project src/Bielu.Ef.Examples.Api \
  --context ProfileDbContext \
  --output-dir Migrations
```

---

## Why This Pattern?

In large applications, it's common to split a single monolith DbContext into bounded-context-specific ones (following Domain-Driven Design). The challenge is that different bounded contexts often share **identity** data (users). This example shows how to:

1. Keep each context's **migration responsibility clean and focused**
2. Allow **cross-context foreign-key references** via `ExcludeFromMigrations()`
3. Handle **cross-context queries at the application layer** (e.g., enriched blog posts endpoint)
