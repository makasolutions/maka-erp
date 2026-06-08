using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        // Slug unique per level (Slug + ParentId), live rows only.
        builder.HasIndex(x => new { x.Slug, x.ParentId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.ImageUrl).HasMaxLength(512);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.FullPath).HasMaxLength(1024);
        builder.HasIndex(x => x.GoogleCategoryId);
        builder.HasIndex(x => x.RootGoogleCategoryId);

        // Self-referencing tree via ParentId. Children navigation only (no Parent nav).
        builder.HasMany(x => x.Children)
            .WithOne()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
