using FSH.Modules.Parties.Domain.V2.Credit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class CreditMovementConfiguration : IEntityTypeConfiguration<CreditMovement>
{
    public void Configure(EntityTypeBuilder<CreditMovement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CreditMovements");
        builder.HasKey(x => x.Id);

        // Enum como smallint (SPEC §13 `: short`). Diverge del string de v1 — decisión PR-B.
        builder.Property(x => x.Tipo).HasConversion<short>();
        builder.Property(x => x.Monto).HasPrecision(18, 2);
        builder.Property(x => x.SaldoResultante).HasPrecision(18, 2);
        builder.Property(x => x.Motivo).HasMaxLength(512);
        builder.Property(x => x.RegistradoPor).IsRequired().HasMaxLength(128);

        // ix_creditmov_account (SPEC §15) — consulta del historial por cuenta y fecha.
        builder.HasIndex(x => new { x.CreditAccountId, x.FechaUtc })
            .HasDatabaseName("ix_creditmov_account");

        builder.Ignore(x => x.DomainEvents);
    }
}
