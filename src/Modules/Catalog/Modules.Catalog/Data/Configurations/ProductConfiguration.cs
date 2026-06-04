using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.ShortDescription).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(50_000);
        builder.Property(x => x.TechnicalSpecs).HasMaxLength(10_000);
        builder.Property(x => x.Specs).HasColumnType("jsonb");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.WeightUnit).HasConversion<string>().HasMaxLength(4).IsRequired();
        builder.Property(x => x.DimensionUnit).HasConversion<string>().HasMaxLength(4).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(18, 4);
        builder.Property(x => x.DimensionLength).HasPrecision(18, 4);
        builder.Property(x => x.DimensionWidth).HasPrecision(18, 4);
        builder.Property(x => x.DimensionHeight).HasPrecision(18, 4);

        builder.Property(x => x.SeoTitle).HasMaxLength(60);
        builder.Property(x => x.SeoDescription).HasMaxLength(160);
        builder.Property(x => x.SeoKeywords).HasMaxLength(512);

        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Child collections — cascade with the product.
        builder.HasMany(x => x.Images).WithOne().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Codes).WithOne().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Tags).WithOne().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ProductCategories).WithOne().HasForeignKey(pc => pc.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attributes).WithOne().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Variations).WithOne().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.BundleItems).WithOne().HasForeignKey(b => b.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.BrandId);
        builder.HasIndex(x => x.TaxRateId);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
