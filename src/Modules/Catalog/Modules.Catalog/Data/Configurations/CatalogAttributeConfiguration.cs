using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class CatalogAttributeConfiguration : IEntityTypeConfiguration<CatalogAttribute>
{
    public void Configure(EntityTypeBuilder<CatalogAttribute> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CatalogAttributes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.IsVisibleOnProduct).IsRequired();
        builder.Property(x => x.IsUsedForVariations).IsRequired();

        builder.HasMany(x => x.Values)
            .WithOne()
            .HasForeignKey(v => v.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OwnerId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class CatalogAttributeValueConfiguration : IEntityTypeConfiguration<CatalogAttributeValue>
{
    public void Configure(EntityTypeBuilder<CatalogAttributeValue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CatalogAttributeValues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ColorCode).HasMaxLength(9);
        builder.Property(x => x.ImageUrl).HasMaxLength(512);
        builder.HasIndex(x => x.AttributeId);
        builder.Ignore(x => x.DomainEvents);
    }
}
