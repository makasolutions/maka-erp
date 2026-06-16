using FSH.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Parties");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdentificationTypeCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IdentificationNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FirstName).HasMaxLength(120);
        builder.Property(x => x.LastName).HasMaxLength(120);
        builder.Property(x => x.ActividadEconomicaCiiuCode).HasMaxLength(64);
        builder.Property(x => x.CreditDaysCode).HasMaxLength(64);
        builder.Property(x => x.TradeName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Website).HasMaxLength(256);
        builder.Property(x => x.TaxRegimeCode).HasMaxLength(64);
        builder.Property(x => x.FiscalResponsibilities).HasMaxLength(512);
        builder.Property(x => x.SourceCode).HasMaxLength(64);
        builder.Property(x => x.MarketingType).HasMaxLength(64);
        builder.Property(x => x.GenderCode).HasMaxLength(64);
        builder.Property(x => x.MaritalStatusCode).HasMaxLength(64);
        builder.Property(x => x.CreditCurrency).HasMaxLength(3);
        builder.Property(x => x.CreditLimit).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Roles).HasConversion<int>();

        // El índice ÚNICO por (TenantId, tipo, número) se define en PartiesDbContext
        // tras base.OnModelCreating, porque el shadow TenantId aún no existe aquí.
        builder.HasIndex(x => x.LegalName);
        builder.HasIndex(x => x.Roles);
        builder.HasIndex(x => x.AssignedUserId);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Contacts).WithOne().HasForeignKey(c => c.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Channels).WithOne().HasForeignKey(c => c.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Team).WithOne().HasForeignKey(m => m.PartyId).OnDelete(DeleteBehavior.Cascade);

        // ──────────────────────────────────────────────────────────────────────────────
        // PRIMER OwnsOne NULLABLE del repo (PR-D1) — patrón de referencia para owned VOs
        // OPCIONALES (lo copiará D4 para LegalRepresentative). FiscalData v2 (ejes fiscales
        // separados, SPEC §4) se mapea INLINE en la tabla Parties con prefijo FiscalData_*.
        //
        // Claves del patrón owned NULLABLE (vs el PaymentTerms REQUERIDO de PR-B):
        //   1. OwnsOne(...) con columnas inline.
        //   2. builder.Navigation(x => x.Vo).IsRequired(false) → EF materializa null cuando
        //      las columnas están vacías (en vez de un VO vacío).
        //   3. Enums nullable → HasConversion<short>() (smallint nullable).
        //   4. Colección de strings (ResponsabilidadesFiscales, códigos DIAN R-99-PN/O-13…) →
        //      ValueConverter a string con separador + ValueComparer (EF necesita el comparer
        //      para detectar cambios en colecciones).
        // ──────────────────────────────────────────────────────────────────────────────
        var dianCodesConverter = new ValueConverter<IReadOnlyList<string>, string>(
            v => string.Join(',', v),
            v => string.IsNullOrEmpty(v)
                ? new List<string>()
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var dianCodesComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (acc, s) => HashCode.Combine(acc, StringComparer.Ordinal.GetHashCode(s))),
            v => v.ToList());

        builder.OwnsOne(x => x.FiscalData, fd =>
        {
            fd.Property(p => p.RegimenTributario).HasConversion<short?>().HasColumnName("FiscalData_RegimenTributario");
            fd.Property(p => p.ResponsabilidadIVA).HasConversion<short?>().HasColumnName("FiscalData_ResponsabilidadIVA");
            fd.Property(p => p.FormaJuridica).HasMaxLength(64).HasColumnName("FiscalData_FormaJuridica");
            fd.Property(p => p.GranContribuyente).HasColumnName("FiscalData_GranContribuyente");
            fd.Property(p => p.Autorretenedor).HasColumnName("FiscalData_Autorretenedor");
            fd.Property(p => p.AgenteRetencionIVA).HasColumnName("FiscalData_AgenteRetencionIVA");
            fd.Property(p => p.AgenteRetencionICA).HasColumnName("FiscalData_AgenteRetencionICA");
            fd.Property(p => p.ObligadoLlevarContabilidad).HasColumnName("FiscalData_ObligadoLlevarContabilidad");
            fd.Property(p => p.FlagPEP).HasColumnName("FiscalData_FlagPEP");
            fd.Property(p => p.ResponsabilidadesFiscales)
                .HasConversion(dianCodesConverter, dianCodesComparer)
                .HasColumnName("FiscalData_ResponsabilidadesFiscales")
                .HasMaxLength(512);
        });
        builder.Navigation(x => x.FiscalData).IsRequired(false);

        builder.Ignore(x => x.DomainEvents);
    }
}
