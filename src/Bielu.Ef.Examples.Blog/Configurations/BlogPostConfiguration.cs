using Bielu.Ef.Examples.Blog.Entities;
using Bielu.Ef.Examples.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bielu.Ef.Examples.Blog.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(600);
        builder.HasIndex(p => p.Slug).IsUnique();

        // FK to Users table owned by AuthDbContext.
        // We define the relationship here so EF can resolve queries,
        // but the Users table itself is excluded from migrations (see BlogDbContext).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Versions)
            .WithOne(v => v.BlogPost)
            .HasForeignKey(v => v.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
