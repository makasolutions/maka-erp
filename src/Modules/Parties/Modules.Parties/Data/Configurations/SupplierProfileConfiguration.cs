using FSH.Modules.Parties.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class SupplierProfileConfiguration : IEntityTypeConfiguration<SupplierProfile>
{
    public void Configure(EntityTypeBuilder<SupplierProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SupplierProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LeadTimeDays);
        // Índice ÚNICO sobre PartyId lo crea la relación one-to-one en PartyConfiguration (PR-D2).

        // ──────────────────────────────────────────────────────────────────────────────
        // PRIMER OwnsOne DEL REPO — patrón de referencia para owned VOs (precedente PR-D).
        // PaymentTerms es un value object (sealed record) mapeado INLINE en la misma tabla
        // SupplierProfiles. EF genera columnas con prefijo: PaymentTerms_DiasCredito,
        // PaymentTerms_FormaPagoId. Es un owned REQUERIDO (la propiedad nunca es null:
        // default PaymentTerms.None), por eso no se marca IsRequired(false).
        //
        // PR-D copiará este patrón para FiscalData y LegalRepresentative como owned de Party.
        // Claves del patrón:
        //   1. OwnsOne(x => x.Vo, vo => { vo.Property(...); }) — columnas inline, no tabla aparte.
        //   2. El tipo owned NO necesita Id ni configuración de tabla propia.
        //   3. Para owned NULLABLE (LegalRepresentative?), EF crea columnas nullable y materializa
        //      el VO solo si alguna columna es no-nula (configurar con .Navigation().IsRequired(false)).
        // ──────────────────────────────────────────────────────────────────────────────
        builder.OwnsOne(x => x.PaymentTerms, pt =>
        {
            pt.Property(p => p.DiasCredito).HasColumnName("PaymentTerms_DiasCredito");
            pt.Property(p => p.FormaPagoId).HasColumnName("PaymentTerms_FormaPagoId");
        });

        builder.Ignore(x => x.DomainEvents);
    }
}
