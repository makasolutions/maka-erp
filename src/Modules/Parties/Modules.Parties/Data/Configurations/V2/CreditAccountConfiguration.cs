using FSH.Modules.Parties.Domain.V2.Credit;
using FSH.Modules.Parties.Domain.V2.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class CreditAccountConfiguration : IEntityTypeConfiguration<CreditAccount>
{
    public void Configure(EntityTypeBuilder<CreditAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CreditAccounts");
        builder.HasKey(x => x.Id);

        // CreditAccount tiene TenantId EXPLÍCITO (SPEC §9 "por compañía"). En un BaseDbContext,
        // Finbuckle adopta una propiedad existente llamada TenantId como el discriminador de tenant
        // (no agrega shadow). Lo mapeamos como columna normal; base.OnModelCreating aplica IsMultiTenant.
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.MonedaId).IsRequired().HasMaxLength(3);
        builder.Property(x => x.AprobadoPor).HasMaxLength(128);
        builder.Property(x => x.CupoAsignado).HasPrecision(18, 2);
        builder.Property(x => x.SaldoDisponible).HasPrecision(18, 2);

        // FK a CustomerProfile con RESTRICT (PR-D2): un cliente con historial de crédito NO puede
        // borrarse. Como Party→CustomerProfile es Cascade, esto bloquea transitivamente el borrado
        // de un Party con CreditAccount — protege el log inmutable de movimientos (opción "b",
        // más correcta que dejar el CreditAccount huérfano). La FK crea su índice sobre CustomerProfileId.
        builder.HasOne<CustomerProfile>()
            .WithMany()
            .HasForeignKey(x => x.CustomerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Historial de movimientos (append-only). Backing field _movements.
        builder.HasMany(x => x.Movements)
            .WithOne()
            .HasForeignKey(m => m.CreditAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Movements)
            .HasField("_movements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
