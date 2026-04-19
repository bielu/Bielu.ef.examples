using Bielu.Ef.Examples.Blog.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bielu.Ef.Examples.Blog.Configurations;

public class BlogPostVersionConfiguration : IEntityTypeConfiguration<BlogPostVersion>
{
    public void Configure(EntityTypeBuilder<BlogPostVersion> builder)
    {
        builder.ToTable("BlogPostVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Content).IsRequired();
        builder.Property(v => v.Summary).HasMaxLength(1000);

        // FK to Users table owned by AuthDbContext.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
