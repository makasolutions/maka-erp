using FSH.Modules.NamingSeries.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.NamingSeries.Data.Configurations;

public sealed class NamingSeriesConfiguration : IEntityTypeConfiguration<FSH.Modules.NamingSeries.Domain.NamingSeries>
{
    public void Configure(EntityTypeBuilder<FSH.Modules.NamingSeries.Domain.NamingSeries> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("NamingSeries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DocumentType).IsRequired().HasMaxLength(64);

        // Pattern persistido como string; al cargar se reconstruye el VO via Parse.
        builder.Property(x => x.Pattern)
            .HasConversion(
                v => v.Raw,
                v => NamingPattern.Parse(v))
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.From).IsRequired();
        builder.Property(x => x.To).IsRequired();
        builder.Property(x => x.CurrentValue).IsRequired();
        builder.Property(x => x.Notified80Pct).IsRequired();

        builder.Property(x => x.ResolutionNumber).HasMaxLength(64);
        builder.Property(x => x.ResolutionTechnicalKey).HasMaxLength(128);
        builder.Property(x => x.ClosedBy).HasMaxLength(128);
        builder.Property(x => x.CreatedBy).HasMaxLength(128);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(128);

        // Único activo por (TenantId, DocumentType): DIAN exige una sola serie vigente por tipo.
        builder.HasIndex(x => new { x.TenantId, x.DocumentType })
            .HasFilter("\"ClosedAtUtc\" IS NULL")
            .HasDatabaseName("ix_namingseries_active_tenant_doctype")
            .IsUnique();

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_namingseries_tenant");

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.Capacity);
        builder.Ignore(x => x.IsOpen);
    }
}
