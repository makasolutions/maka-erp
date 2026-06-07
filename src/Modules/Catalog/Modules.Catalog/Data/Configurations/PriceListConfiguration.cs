using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PriceLists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.CustomerSegment).IsRequired().HasMaxLength(32);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.AdjustmentPercent).HasPrecision(7, 4);
        builder.Property(x => x.ListKind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CampaignStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.StartJobId).HasMaxLength(64);
        builder.Property(x => x.EndJobId).HasMaxLength(64);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Find the active list for a segment fast.
        builder.HasIndex(x => new { x.CustomerSegment, x.OwnerId, x.IsActive });
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PriceListItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Price).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.IsManualOverride).IsRequired();
        builder.Property(x => x.PreCampaignPrice).HasPrecision(18, 4);
        builder.Property(x => x.MinQuantity).HasPrecision(18, 4);
        builder.Property(x => x.SalePrice).HasPrecision(18, 4);
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64);

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(h => h.PriceListItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.VariationId);
        builder.HasIndex(x => new { x.PriceListId, x.VariationId });
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PriceListItemHistoryConfiguration : IEntityTypeConfiguration<PriceListItemHistory>
{
    public void Configure(EntityTypeBuilder<PriceListItemHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PriceListItemHistory");
        builder.HasKey(x => x.Id);
        // History rows are created with a client-generated Guid (Guid.CreateVersion7)
        // and ALWAYS added through the PriceListItem.History navigation of an already-
        // tracked parent (via ChangePrice). Without ValueGeneratedNever, EF's default
        // Guid-key convention (ValueGeneratedOnAdd) sees the non-default key on a
        // navigation-added child and marks it Modified instead of Added → the UPDATE
        // affects 0 rows → DbUpdateConcurrencyException. ValueGeneratedNever makes EF
        // treat the new instance as Added.
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OldPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.NewPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.OldSalePrice).HasPrecision(18, 4);
        builder.Property(x => x.NewSalePrice).HasPrecision(18, 4);
        builder.Property(x => x.ChangedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ChangeReason).HasMaxLength(256);
        builder.Property(x => x.SourceReference).HasMaxLength(256);
        builder.HasIndex(x => x.PriceListItemId);
        builder.HasIndex(x => x.VariationId);
        builder.Ignore(x => x.DomainEvents);
    }
}
