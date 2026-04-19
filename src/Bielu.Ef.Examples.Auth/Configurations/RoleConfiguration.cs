using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bielu.Ef.Examples.Auth.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Name).IsUnique();

        // Seed default roles
        builder.HasData(
            new Role { Id = 1, Name = "Admin", Description = "Administrator with full access" },
            new Role { Id = 2, Name = "User", Description = "Standard user" },
            new Role { Id = 3, Name = "Author", Description = "Can create and publish blog posts" }
        );
    }
}
