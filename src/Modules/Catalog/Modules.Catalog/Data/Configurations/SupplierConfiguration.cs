using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class SupplierBrandConfiguration : IEntityTypeConfiguration<SupplierBrand>
{
    public void Configure(EntityTypeBuilder<SupplierBrand> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SupplierBrands");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.BrandId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class SupplierCategoryConfiguration : IEntityTypeConfiguration<SupplierCategory>
{
    public void Configure(EntityTypeBuilder<SupplierCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SupplierCategories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => new { x.SupplierId, x.CategoryId }).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class SupplierProductConfiguration : IEntityTypeConfiguration<SupplierProduct>
{
    public void Configure(EntityTypeBuilder<SupplierProduct> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SupplierProducts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.CostCurrency).HasMaxLength(3);
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.VariationId);
        builder.Ignore(x => x.DomainEvents);
    }
}
