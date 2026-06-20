using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Lookups.Data.Configurations;

public sealed class BasicRecordConfiguration : IEntityTypeConfiguration<BasicRecord>
{
    public void Configure(EntityTypeBuilder<BasicRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("BasicRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(64);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(256);

        builder.Property(x => x.IsProtected).HasDefaultValue(false);

        builder.HasIndex(x => x.BasicTableId);
        // Código único por tabla.
        builder.HasIndex(x => new { x.BasicTableId, x.Code }).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
