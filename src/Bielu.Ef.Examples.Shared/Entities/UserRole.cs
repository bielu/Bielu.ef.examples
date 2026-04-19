namespace Bielu.Ef.Examples.Shared.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between User and Role.
/// Owned and migrated by AuthDbContext.
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Role Role { get; set; } = null!;
}
