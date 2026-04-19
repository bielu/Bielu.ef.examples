namespace Bielu.Ef.Examples.Profile.Entities;

/// <summary>
/// Represents extended profile information for a user.
/// Owned and migrated by ProfileDbContext.
/// </summary>
public class UserProfile
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the Users table managed by AuthDbContext.
    /// ProfileDbContext knows about this FK but does NOT own the Users table migration.
    /// </summary>
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
