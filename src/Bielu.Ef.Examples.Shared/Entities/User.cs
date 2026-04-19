namespace Bielu.Ef.Examples.Shared.Entities;

/// <summary>
/// Represents an application user. This entity is owned and migrated by AuthDbContext.
/// BlogDbContext and ProfileDbContext reference this entity but exclude it from their migrations.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
