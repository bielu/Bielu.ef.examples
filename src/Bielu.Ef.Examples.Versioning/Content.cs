using Bielu.EntityFramework.Extensions.Versioning.Abstractions;

namespace Bielu.Ef.Examples.Versioning;

/// <summary>
/// Versioned aggregate that demonstrates the bielu EF Core versioning library.
///
/// Each <see cref="Content"/> aggregate has a stable <see cref="VersionedEntity{TEntityId, TVersionId}.EntityId"/>
/// shared by every version, while every individual version row carries its
/// own <see cref="VersionedEntity{TEntityId, TVersionId}.VersionId"/> primary
/// key. The base class also supplies <c>EffectiveAt</c>, <c>RecordedAt</c>,
/// <c>VersionNumber</c>, <c>IsDeleted</c> and a concurrency token out of the
/// box, so the only thing this class needs to define is the actual payload.
/// </summary>
public sealed class Content : VersionedEntity<Guid, Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
