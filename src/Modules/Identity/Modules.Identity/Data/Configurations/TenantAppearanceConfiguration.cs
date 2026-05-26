using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data.Configurations;

public class TenantAppearanceConfiguration : IEntityTypeConfiguration<TenantAppearance>
{
    public void Configure(EntityTypeBuilder<TenantAppearance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("TenantAppearances", IdentityModuleConstants.SchemaName)
            .HasKey(a => a.Id);

        builder
            .Property(a => a.Theme)
            .IsRequired()
            .HasMaxLength(10);

        builder
            .Property(a => a.Accent)
            .IsRequired()
            .HasMaxLength(50);

        builder
            .Property(a => a.Font)
            .IsRequired()
            .HasMaxLength(50);

        builder
            .Property(a => a.Density)
            .IsRequired()
            .HasMaxLength(20);

        builder
            .Property(a => a.CustomAccentJson)
            .HasMaxLength(4000);

        builder
            .Property(a => a.CreatedBy)
            .HasMaxLength(256);

        builder
            .Property(a => a.LastModifiedBy)
            .HasMaxLength(256);

        // Unique per tenant — enforced by Finbuckle global query filter + single row design.
        // We do not add a unique index because TenantId is injected transparently.
    }
}
