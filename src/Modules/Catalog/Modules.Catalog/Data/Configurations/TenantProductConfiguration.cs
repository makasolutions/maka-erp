using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class TenantProductConfiguration : IEntityTypeConfiguration<TenantProduct>
{
    public void Configure(EntityTypeBuilder<TenantProduct> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TenantProducts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameOverride).HasMaxLength(200);
        builder.Property(x => x.ShortDescriptionOverride).HasMaxLength(500);
        builder.Property(x => x.SeoTitleOverride).HasMaxLength(60);
        builder.Property(x => x.SeoDescriptionOverride).HasMaxLength(160);
        builder.Property(x => x.DropshippingPrice).HasPrecision(18, 4);
        builder.Property(x => x.DropshippingMinQty).HasPrecision(18, 4);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsPublic).IsRequired();

        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(i => i.TenantProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CanonicalProductId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class TenantProductImageConfiguration : IEntityTypeConfiguration<TenantProductImage>
{
    public void Configure(EntityTypeBuilder<TenantProductImage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TenantProductImages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(512);
        builder.Property(x => x.AltText).HasMaxLength(256);
        builder.HasIndex(x => x.TenantProductId);
        builder.Ignore(x => x.DomainEvents);
    }
}
