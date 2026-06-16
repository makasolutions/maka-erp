using FSH.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class PartyCiiuActivityConfiguration : IEntityTypeConfiguration<PartyCiiuActivity>
{
    public void Configure(EntityTypeBuilder<PartyCiiuActivity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyCiiuActivities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CiiuCode).IsRequired().HasMaxLength(8);
        builder.HasIndex(x => x.PartyId);

        // ix_ciiu_principal (SPEC §15) — único parcial: garantiza ≤1 principal por Party.
        // PartyId es Guid global, así que (PartyId) WHERE IsPrincipal basta (no requiere TenantId).
        builder.HasIndex(x => x.PartyId)
            .IsUnique()
            .HasFilter("\"IsPrincipal\" = TRUE")
            .HasDatabaseName("ix_ciiu_principal");

        builder.Ignore(x => x.DomainEvents);
    }
}
