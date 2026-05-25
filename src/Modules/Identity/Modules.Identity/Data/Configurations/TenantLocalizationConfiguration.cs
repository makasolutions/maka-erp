using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data.Configurations;

public class TenantLocalizationConfiguration : IEntityTypeConfiguration<TenantLocalization>
{
    public void Configure(EntityTypeBuilder<TenantLocalization> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("TenantLocalizations", IdentityModuleConstants.SchemaName)
            .HasKey(l => l.Id);

        builder
            .Property(l => l.Timezone)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(l => l.DateFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder
            .Property(l => l.TimeFormat)
            .IsRequired()
            .HasMaxLength(5);

        builder
            .Property(l => l.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder
            .Property(l => l.Language)
            .IsRequired()
            .HasMaxLength(10);

        builder
            .Property(l => l.NumberFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder
            .Property(l => l.CreatedBy)
            .HasMaxLength(256);

        builder
            .Property(l => l.LastModifiedBy)
            .HasMaxLength(256);

        // One row per tenant (UserId = null). Future per-user override: UserId != null.
        builder.HasIndex(l => l.UserId);
    }
}
