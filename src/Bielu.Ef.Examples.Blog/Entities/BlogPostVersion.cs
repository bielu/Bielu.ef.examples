namespace Bielu.Ef.Examples.Blog.Entities;

/// <summary>
/// Represents a specific version (snapshot) of a blog post's content.
/// Owned and migrated by BlogDbContext.
/// </summary>
public class BlogPostVersion
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The user who created this version. FK to the Users table owned by AuthDbContext.
    /// </summary>
    public int CreatedByUserId { get; set; }

    // Navigation property back to the post
    public BlogPost BlogPost { get; set; } = null!;
}
