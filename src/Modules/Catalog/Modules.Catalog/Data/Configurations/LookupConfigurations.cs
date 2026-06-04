using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TaxRates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Rate).HasPrecision(6, 4).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.OwnerId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ShippingClassConfiguration : IEntityTypeConfiguration<ShippingClass>
{
    public void Configure(EntityTypeBuilder<ShippingClass> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ShippingClasses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(256);
        builder.HasIndex(x => x.OwnerId);
        builder.Ignore(x => x.DomainEvents);
    }
}
