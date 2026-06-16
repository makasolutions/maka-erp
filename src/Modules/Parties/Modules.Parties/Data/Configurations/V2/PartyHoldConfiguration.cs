using FSH.Modules.Parties.Domain.V2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class PartyHoldConfiguration : IEntityTypeConfiguration<PartyHold>
{
    public void Configure(EntityTypeBuilder<PartyHold> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyHolds");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HoldType).HasConversion<short>(); // smallint (SPEC §13)
        builder.Property(x => x.Motivo).IsRequired().HasMaxLength(512);
        builder.Property(x => x.CreadoPor).IsRequired().HasMaxLength(128);

        // ix_holds_active (SPEC §15) — índice parcial: solo holds activos (la consulta caliente
        // "¿hay bloqueo vigente para este party/tipo?" del flujo OV/OC/Pago).
        builder.HasIndex(x => new { x.PartyId, x.HoldType })
            .HasFilter("\"EstaActivo\" = TRUE")
            .HasDatabaseName("ix_holds_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
