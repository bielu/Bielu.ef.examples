using Bielu.EntityFramework.Extensions.Versioning.Abstractions;
using Bielu.EntityFramework.Extensions.Versioning.Registration;
using Bielu.Ef.Examples.Versioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// ------------------------------------------------------------------------------------
// Bielu EF Core versioning library — Postgres edition
//
// This sample shows how to combine the bielu content-versioning subsystem with
// .NET Aspire's Postgres client integration. The wiring is split into two
// independent steps so that each piece is replaceable on its own:
//
//   1. AddBieluVersioning() registers the cross-cutting versioning services
//      (clock, options, save-changes interceptor) in DI. The interceptor is
//      surfaced as IInterceptor and is therefore auto-discovered by EF Core
//      as soon as the DbContext is built with UseApplicationServiceProvider
//      — which Aspire's AddNpgsqlDbContext does for us.
//
//   2. AddNpgsqlDbContext<ContentDbContext>(...) registers the DbContext
//      against the "biele-ef-examples-db" Postgres resource provisioned by
//      the Aspire AppHost and tells EF Core that the Npgsql migrations live
//      inside this project (so the same ContentDbContext type can be reused
//      with other providers without sharing migration history).
//
// The result is a fully-fledged versioned DbContext on Postgres without
// having to touch any provider-specific feature: no temporal tables, no
// triggers, no raw SQL — the same model would also work on SqlServer,
// MySQL, SQLite, Cosmos or InMemory unchanged.
// ------------------------------------------------------------------------------------

const string ConnectionName = "biele-ef-examples-db";
var migrationsAssembly = typeof(Program).Assembly.GetName().Name!;

builder.Services.AddBieluVersioning();

builder.AddNpgsqlDbContext<ContentDbContext>(
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
// Apply Postgres migrations on startup. The migrations live inside this
// project under /Migrations and are wired through MigrationsAssembly above so
// the bielu-defined ContentDbContext model produces a real, source-controlled
// Postgres schema (instead of relying on EnsureCreated).
// ------------------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
    await ctx.Database.MigrateAsync();
}

// ------------------------------------------------------------------------------------
// Minimal API surface mirrors the upstream bielu sample so the same client
// code can be pointed at either example. Every endpoint goes through the
// VersionedDbContext instance helpers (db.SaveAsync/db.GetCurrentAsync/...)
// which delegate to the same engine as the DbSet<T> extension methods.
// ------------------------------------------------------------------------------------

// POST /content/{id}?effectiveAt=...   — create or append a new version.
//
// Returns the result kind so callers can see whether the write produced the
// initial version, was appended to the head of the timeline, or was inserted
// between two existing versions (Archive).
app.MapPost("/content/{id:guid}", async (
    Guid id,
    DateTimeOffset? effectiveAt,
    Content payload,
    ContentDbContext db,
    IVersioningClock clock) =>
{
    var result = await db.SaveAsync<Content, Guid, Guid>(id, effectiveAt ?? clock.UtcNow, payload);
    return Results.Ok(new
    {
        result.Kind,
        result.Entity.EntityId,
        result.Entity.VersionId,
        result.Entity.VersionNumber,
        result.Entity.EffectiveAt,
        result.Entity.RecordedAt
    });
})
.WithName("SaveContentVersion");

// GET /content/{id}?asOf=...           — current version honouring the timeline.
app.MapGet("/content/{id:guid}", async (
    Guid id,
    DateTimeOffset? asOf,
    ContentDbContext db) =>
{
    var current = await db.GetCurrentAsync<Content, Guid, Guid>(id, asOf);
    return current is null ? Results.NotFound() : Results.Ok(current);
})
.WithName("GetCurrentContent");

// GET /content/{id}/history            — full ordered timeline (oldest first).
app.MapGet("/content/{id:guid}/history", async (
    Guid id,
    ContentDbContext db) =>
        Results.Ok(await db.GetAllVersionsAsync<Content, Guid, Guid>(id)))
    .WithName("GetContentHistory");

// GET /content/{id}/count              — total number of versions.
app.MapGet("/content/{id:guid}/count", async (
    Guid id,
    ContentDbContext db) =>
        Results.Ok(new { count = await db.GetVersionCountAsync<Content, Guid, Guid>(id) }))
    .WithName("GetContentVersionCount");

// DELETE /content/{id}?effectiveAt=... — write a soft-delete tombstone.
app.MapDelete("/content/{id:guid}", async (
    Guid id,
    DateTimeOffset? effectiveAt,
    ContentDbContext db,
    IVersioningClock clock) =>
{
    var result = await db.SoftDeleteVersionAsync<Content, Guid, Guid>(id, effectiveAt ?? clock.UtcNow, new Content());
    return Results.Ok(new { result.Kind, result.Entity.VersionId, result.Entity.VersionNumber });
})
.WithName("SoftDeleteContent");

app.Run();

// Make the implicit Program class visible to the AppHost's source generator.
public partial class Program;
