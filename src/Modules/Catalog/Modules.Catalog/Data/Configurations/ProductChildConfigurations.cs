using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(512);
        builder.Property(x => x.AltText).HasMaxLength(256);
        builder.HasIndex(x => x.ProductId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProductCodeConfiguration : IEntityTypeConfiguration<ProductCode>
{
    public void Configure(EntityTypeBuilder<ProductCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeType).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(128);
        // Unique per variation + code type + value (spec §2.9).
        builder.HasIndex(x => new { x.VariationId, x.CodeType, x.Code }).IsUnique();
        // Match index for bulk supplier price imports.
        builder.HasIndex(x => new { x.SupplierId, x.Code });
        builder.HasIndex(x => x.ProductId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductTags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Color).HasMaxLength(9);
        builder.HasIndex(x => x.ProductId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductCategories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => x.CategoryId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductAttributes");
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.SelectedValues)
            .WithMany()
            .UsingEntity<ProductAttributeValue>(
                r => r.HasOne<CatalogAttributeValue>().WithMany().HasForeignKey(j => j.AttributeValueId).OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<ProductAttribute>().WithMany().HasForeignKey(j => j.ProductAttributeId).OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("ProductAttributeValues");
                    j.HasKey(x => new { x.ProductAttributeId, x.AttributeValueId });
                });
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.AttributeId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProductBundleItemConfiguration : IEntityTypeConfiguration<ProductBundleItem>
{
    public void Configure(EntityTypeBuilder<ProductBundleItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductBundleItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.DiscountFixed).HasPrecision(18, 4);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ItemVariationId);
        builder.Ignore(x => x.DomainEvents);
    }
}
