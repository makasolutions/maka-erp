using FSH.Modules.NamingSeries.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.NamingSeries.Data.Configurations;

public sealed class NamingSeriesAllocationConfiguration : IEntityTypeConfiguration<NamingSeriesAllocation>
{
    public void Configure(EntityTypeBuilder<NamingSeriesAllocation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("NamingSeriesAllocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Number).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DocumentType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AllocatedBy).IsRequired().HasMaxLength(128);

        builder.HasIndex(x => x.NamingSeriesId).HasDatabaseName("ix_namingallocations_series");
        builder.HasIndex(x => new { x.NamingSeriesId, x.Number }).IsUnique()
            .HasDatabaseName("ix_namingallocations_series_number");
        builder.HasIndex(x => x.DocumentId).HasDatabaseName("ix_namingallocations_documentid");

        builder.Ignore(x => x.DomainEvents);
    }
}
