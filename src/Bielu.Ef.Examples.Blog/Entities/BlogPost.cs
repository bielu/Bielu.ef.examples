namespace Bielu.Ef.Examples.Blog.Entities;

/// <summary>
/// Represents a blog post. Owned and migrated by BlogDbContext.
/// </summary>
public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key referencing the Users table, which is managed by AuthDbContext.
    /// BlogDbContext knows about this FK but does NOT own the Users table migration.
    /// </summary>
    public int AuthorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Navigation properties within BlogDbContext scope
    public ICollection<BlogPostVersion> Versions { get; set; } = new List<BlogPostVersion>();
}
