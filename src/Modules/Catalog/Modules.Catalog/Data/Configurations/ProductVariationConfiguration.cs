using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ProductVariationConfiguration : IEntityTypeConfiguration<ProductVariation>
{
    public void Configure(EntityTypeBuilder<ProductVariation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProductVariations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku).IsRequired().HasMaxLength(64);
        // Unique across live rows; per-tenant via shadow TenantId (AdjustUniqueIndexes).
        // The spec's "global" SKU uniqueness belongs to the future canonical layer.
        builder.HasIndex(x => x.Sku).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(256);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.WeightUnit).HasConversion<string>().HasMaxLength(4);
        builder.Property(x => x.Weight).HasPrecision(18, 4);
        builder.Property(x => x.DimensionLength).HasPrecision(18, 4);
        builder.Property(x => x.DimensionWidth).HasPrecision(18, 4);
        builder.Property(x => x.DimensionHeight).HasPrecision(18, 4);
        builder.Property(x => x.ImageUrl).HasMaxLength(512);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Codes owned by this variation (ProductCode.VariationId). The Product→Codes
        // (ProductId) relationship already owns the cascade; this side is Restrict to
        // avoid multiple cascade paths into ProductCode.
        builder.HasMany(x => x.Codes)
            .WithOne()
            .HasForeignKey(c => c.VariationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Attribute combination that defines this variation (M:N via explicit join).
        builder.HasMany(x => x.AttributeValues)
            .WithMany()
            .UsingEntity<VariationAttributeValue>(
                r => r.HasOne<CatalogAttributeValue>().WithMany().HasForeignKey(j => j.AttributeValueId).OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<ProductVariation>().WithMany().HasForeignKey(j => j.VariationId).OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("VariationAttributeValues");
                    j.HasKey(x => new { x.VariationId, x.AttributeValueId });
                });

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
