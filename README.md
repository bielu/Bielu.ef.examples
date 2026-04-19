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
| `Bielu.Ef.Examples.Api` | ASP.NET Core Web API | SQLite demo API wiring all contexts; applies migrations on startup |
| `Bielu.Ef.Examples.Api.Postgres` | ASP.NET Core Web API | **Postgres** demo API wiring all contexts; uses Npgsql + Aspire client integration |
| `Bielu.Ef.Examples.Versioning` | Class Library | `ContentDbContext` + `Content` aggregate using the [bielu EF Core versioning library](https://github.com/bielu/bielu.entityframework.extensions) |
| `Bielu.Ef.Examples.Api.Postgres.Versioning` | ASP.NET Core Web API | **Postgres** demo API for the bielu versioning library; exposes Save/Get/history/count endpoints |
| `Bielu.Ef.Examples.AppHost` | .NET Aspire AppHost | Orchestrates a Postgres container + pgAdmin and the Postgres API |
| `Bielu.Ef.Examples.ServiceDefaults` | Class Library | Shared Aspire service defaults (OpenTelemetry, health checks, service discovery) |

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

- .NET 10 SDK
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`
- For the Aspire / Postgres example: a container runtime (Docker Desktop or Podman)

### Run (SQLite)

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

## Run (Postgres via .NET Aspire)

A second example, `Bielu.Ef.Examples.Api.Postgres`, uses **PostgreSQL** instead of
SQLite while reusing the exact same `AuthDbContext` / `BlogDbContext` /
`ProfileDbContext` class libraries. It is orchestrated by a .NET Aspire AppHost
that provisions a Postgres container, a database, and pgAdmin.

```bash
cd src/Bielu.Ef.Examples.AppHost
dotnet run
```

The AppHost will:

1. Pull and start a `postgres` container (with a persistent data volume)
2. Start a `pgAdmin` container linked to that server
3. Create the `biele-ef-examples-db` database
4. Launch `Bielu.Ef.Examples.Api.Postgres` and inject the connection string via
   Aspire service discovery
5. Apply the Postgres migrations and seed the same demo data as the SQLite example

Open the Aspire dashboard URL printed at startup to inspect resources, logs,
traces and the pgAdmin endpoint.

### Provider-specific migrations

The class libraries (`Auth`, `Blog`, `Profile`) ship with the **SQLite** migrations.
The **Postgres** migrations for the same contexts live in
`src/Bielu.Ef.Examples.Api.Postgres/Migrations/{Auth,Blog,Profile}`. The Postgres
API tells EF Core where to find them by configuring
`UseNpgsql(npgsql => npgsql.MigrationsAssembly("Bielu.Ef.Examples.Api.Postgres"))`
on each `DbContext`. This keeps each provider's migration history isolated.

To add a new Postgres migration for a context:

```bash
# Example: AuthDbContext
dotnet ef migrations add <MigrationName> \
  --project src/Bielu.Ef.Examples.Api.Postgres \
  --startup-project src/Bielu.Ef.Examples.Api.Postgres \
  --context AuthDbContext \
  --output-dir Migrations/Auth
```

Repeat with `--context BlogDbContext --output-dir Migrations/Blog` and
`--context ProfileDbContext --output-dir Migrations/Profile` for the other two.

---

## Content versioning on Postgres (bielu EF versioning library)

A third example, `Bielu.Ef.Examples.Api.Postgres.Versioning`, demonstrates the
[`Bielu.EntityFramework.Extensions.Versioning`](https://github.com/bielu/bielu.entityframework.extensions)
library running on PostgreSQL. The library adds **provider-agnostic content
versioning** to any EF Core model — every aggregate keeps a stable
`EntityId` while each individual revision gets its own `VersionId`, with
support for inserting late / out-of-order updates between two existing
versions. No temporal tables, no triggers, no provider-specific SQL.

The example is split into two projects:

| Project | Type | Responsibility |
|---------|------|----------------|
| `Bielu.Ef.Examples.Versioning` | Class Library | Defines the `Content` versioned aggregate (extends `VersionedEntity<Guid, Guid>`) and `ContentDbContext` (derives from `VersionedDbContext` and calls `modelBuilder.ApplyVersioning<Content, Guid, Guid>(new VersioningOptions())`) |
| `Bielu.Ef.Examples.Api.Postgres.Versioning` | ASP.NET Core Web API | Wires the context against the same Postgres container provisioned by the AppHost; exposes minimal-API endpoints for write/read/history/count/soft-delete |

### Wiring (Aspire + bielu)

The bielu interceptor is registered as a normal `IInterceptor` and is
auto-discovered by EF Core through `UseApplicationServiceProvider`, which
Aspire's `AddNpgsqlDbContext` already wires up. The relevant lines in
`Program.cs` are:

```csharp
builder.Services.AddBieluVersioning();

builder.AddNpgsqlDbContext<ContentDbContext>(
    connectionName: "biele-ef-examples-db",
    configureDbContextOptions: options => options.UseNpgsql());
```

Because `AddBieluVersioning()` is independent from the DbContext
registration, you can keep using Aspire's component-style registration
without giving up provider portability.

### Run

The AppHost launches both Postgres APIs against the same Postgres database:

```bash
cd src/Bielu.Ef.Examples.AppHost
dotnet run
```

The new service is exposed as `api-postgres-versioning` in the Aspire
dashboard.

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST   | `/content/{id}?effectiveAt=...` | Save a new version (Initial / Current / Archive depending on the timeline) |
| POST   | `/content/batch` | Save many versions (across one or many aggregates) in a single transactional round-trip via `SaveManyAsync` |
| GET    | `/content/{id}?asOf=...` | Get the current version (or the version effective at a past timestamp) |
| GET    | `/content/{id}/history` | Full ordered timeline for the aggregate |
| GET    | `/content/{id}/count` | Total number of versions for the aggregate |
| DELETE | `/content/{id}?effectiveAt=...` | Write a soft-delete tombstone preserving history |

### Batch updates

`POST /content/batch` writes many versions in a single transaction by
forwarding the request to `VersionedDbContext.SaveManyAsync`. Each item
targets its own aggregate (by `id`) and may carry its own `effectiveAt`
timestamp; omitted timestamps fall back to "now". Either every version is
persisted or none of them are.

```json
{
  "items": [
    { "id": "11111111-1111-1111-1111-111111111111", "payload": { "title": "Post A v2", "body": "..." } },
    { "id": "22222222-2222-2222-2222-222222222222", "effectiveAt": "2026-01-01T00:00:00Z", "payload": { "title": "Post B v1", "body": "..." } }
  ]
}
```

The response preserves request order and reports the resulting `Kind`
(`Initial` / `Current` / `Archive`) plus the assigned `versionId`,
`versionNumber`, `effectiveAt` and `recordedAt` for each aggregate.

#### Duplicate items in the same batch

`SaveManyAsync` is intentionally permissive — it does **not** deduplicate
items and, unlike the single-item `SaveAsync`, it does **not** run the
per-write "same `effectiveAt` already exists" collision check. The
behaviour you can rely on is:

- **Two items targeting the same aggregate with different `effectiveAt`** —
  both versions are written. `Kind` is classified correctly because the
  engine tracks the running max `effectiveAt` per aggregate in-memory while
  it walks the batch, so an item slotted before the running max is
  reported as `Archive`.
- **Two items targeting the same aggregate with the same `effectiveAt`** —
  both rows are inserted with distinct `versionId`s; the
  `(EntityId, EffectiveAt, VersionId)` unique index allows this. A
  subsequent `GetCurrentAsync` call resolves the winner by tie-breaking on
  `versionNumber`. The single-item endpoint would have rejected this via
  `AnyEffectiveAtCollisionAsync`; the batch endpoint will not.
- **Re-using the same `Content` payload object reference for two items** —
  EF Core change tracking collapses them to a single inserted row, but the
  response still contains two `VersionSaveResult` entries pointing at the
  same `Content` instance.

In short, batches favour throughput over idempotency. If you need
"duplicate-safe" semantics, dedupe upstream (for example by
`(id, effectiveAt)`) before sending the request.

### Schema bootstrap

The schema for the `Content` aggregate is created from a real, source-controlled
EF Core migration shipped inside `Bielu.Ef.Examples.Api.Postgres.Versioning/Migrations/`.
On startup the API calls `ctx.Database.MigrateAsync()` so the table is created
(or upgraded) automatically. The `MigrationsAssembly("Bielu.Ef.Examples.Api.Postgres.Versioning")`
hook on the `UseNpgsql` options keeps this Postgres history isolated from any
other provider you might use the same `ContentDbContext` with.

To add a new Postgres migration for the versioning context:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Bielu.Ef.Examples.Api.Postgres.Versioning \
  --startup-project src/Bielu.Ef.Examples.Api.Postgres.Versioning \
  --context ContentDbContext \
  --output-dir Migrations
```

---

## Why This Pattern?

In large applications, it's common to split a single monolith DbContext into bounded-context-specific ones (following Domain-Driven Design). The challenge is that different bounded contexts often share **identity** data (users). This example shows how to:

1. Keep each context's **migration responsibility clean and focused**
2. Allow **cross-context foreign-key references** via `ExcludeFromMigrations()`
3. Handle **cross-context queries at the application layer** (e.g., enriched blog posts endpoint)
