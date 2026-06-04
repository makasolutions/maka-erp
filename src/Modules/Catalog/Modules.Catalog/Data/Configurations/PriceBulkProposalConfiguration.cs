using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class PriceBulkProposalConfiguration : IEntityTypeConfiguration<PriceBulkProposal>
{
    public void Configure(EntityTypeBuilder<PriceBulkProposal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PriceBulkProposals");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SupplierCode).IsRequired().HasMaxLength(128);
        builder.Property(x => x.OldPrice).HasPrecision(18, 4);
        builder.Property(x => x.NewPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.ChangeReason).HasMaxLength(256);
        builder.Property(x => x.SourceReference).HasMaxLength(256);
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DecidedByUserId).HasMaxLength(64);

        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.SupplierId, x.Status });

        builder.Ignore(x => x.DomainEvents);
    }
}
