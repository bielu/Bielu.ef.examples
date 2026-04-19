namespace Bielu.Ef.Examples.Shared.Entities;

/// <summary>
/// Represents an application role. Owned and migrated by AuthDbContext.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
