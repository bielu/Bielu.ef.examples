using Bielu.EntityFramework.Extensions.Versioning;
using Bielu.EntityFramework.Extensions.Versioning.Abstractions;
using Bielu.EntityFramework.Extensions.Versioning.Modeling;
using Microsoft.EntityFrameworkCore;

namespace Bielu.Ef.Examples.Versioning;

/// <summary>
/// DbContext that opts the <see cref="Content"/> aggregate into the bielu EF
/// Core versioning subsystem.
///
/// Deriving from <see cref="VersionedDbContext"/> makes the
/// <c>SaveAsync</c>/<c>UpsertAsync</c>/<c>GetCurrentAsync</c> instance
/// helpers available directly on the context. The same operations are also
/// exposed as <c>DbSet&lt;T&gt;</c> extension methods (constrained to
/// versioned entities) for callers that prefer working through the set.
///
/// The <see cref="ApplyVersioningExtensions.ApplyVersioning{TEntity, TEntityId, TVersionId}"/>
/// call below sets <c>VersionId</c> as the primary key, adds a composite
/// unique index on <c>(EntityId, EffectiveAt, VersionId)</c> plus an
/// <c>(EntityId, VersionNumber)</c> index, and configures the concurrency
/// token. No raw SQL, no temporal tables, no provider-specific features —
/// the same model works on every EF Core provider, including PostgreSQL.
/// </summary>
public sealed class ContentDbContext(DbContextOptions<ContentDbContext> options)
    : VersionedDbContext(options)
{
    public DbSet<Content> Contents => Set<Content>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyVersioning<Content, Guid, Guid>(new VersioningOptions());
    }
}
