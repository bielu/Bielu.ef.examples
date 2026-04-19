using Bielu.Ef.Examples.Profile.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bielu.Ef.Examples.Profile.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName).HasMaxLength(200);
        builder.Property(p => p.AvatarUrl).HasMaxLength(2048);
        builder.Property(p => p.WebsiteUrl).HasMaxLength(2048);
        builder.Property(p => p.Location).HasMaxLength(200);

        // One-to-one: each user has one profile.
        // FK to Users table owned by AuthDbContext.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
