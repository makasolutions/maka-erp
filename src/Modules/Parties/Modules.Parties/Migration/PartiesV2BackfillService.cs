using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Credit;
using FSH.Modules.Parties.Domain.V2.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Parties.Migration;

/// <summary>
/// Backfill v1→v2 (PR-C, SPEC §0.1). Lee los Party v1 del tenant actual y puebla las tablas v2
/// nuevas (Profiles, CreditAccount+movimiento, PartyCiiuActivity, PartyHold). NO modifica ni borra
/// datos v1 — solo lee de v1 y escribe en v2. Idempotente (guards por existencia de fila v2),
/// tenant-aware (contexto Finbuckle), y con modo dry-run.
///
/// Opera SIEMPRE dentro de un único tenant (el contexto debe estar fijado por el caller). La
/// orquestación multi-tenant vive en el DbMigrator.
/// </summary>
public sealed class PartiesV2BackfillService(PartiesDbContext db, ILogger<PartiesV2BackfillService> logger)
{
    public async Task<BackfillReport> RunAsync(bool dryRun, string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var report = new BackfillReport { DryRun = dryRun };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // v1: solo terceros vivos (el global query filter de ISoftDeletable + tenant ya aplican).
        // TRACKED (no AsNoTracking): PR-D1 escribe Party.FiscalData sobre la misma fila Party, así
        // que el SaveChanges final persiste tanto las filas v2 nuevas como el FiscalData del Party.
        var parties = await db.Parties.ToListAsync(ct).ConfigureAwait(false);

        foreach (var party in parties)
        {
            report.PartiesScanned++;
            Guid? customerProfileId = null;

            // ── Roles → Profiles ───────────────────────────────────────────────
            if (party.IsCustomer)
            {
                var existing = await db.CustomerProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PartyId == party.Id, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    report.CustomerProfilesSkipped++;
                    customerProfileId = existing.Id;
                }
                else
                {
                    var profile = CustomerProfile.Create(party.Id);
                    if (!dryRun) db.CustomerProfiles.Add(profile);
                    report.CustomerProfilesCreated++;
                    customerProfileId = profile.Id;
                }
            }

            if (party.IsSupplier)
            {
                var exists = await db.SupplierProfiles.AsNoTracking()
                    .AnyAsync(p => p.PartyId == party.Id, ct).ConfigureAwait(false);
                if (exists)
                {
                    report.SupplierProfilesSkipped++;
                }
                else
                {
                    // Sin moneda: v1 no tiene catálogo de monedas con Guid (DefaultCurrencyId null → PR-D).
                    if (!dryRun) db.SupplierProfiles.Add(SupplierProfile.Create(party.Id));
                    report.SupplierProfilesCreated++;
                    report.SuppliersWithoutCurrency++;
                }
            }

            if (party.IsEmployee)
            {
                var exists = await db.EmployeeProfiles.AsNoTracking()
                    .AnyAsync(p => p.PartyId == party.Id, ct).ConfigureAwait(false);
                if (exists) report.EmployeeProfilesSkipped++;
                else { if (!dryRun) db.EmployeeProfiles.Add(EmployeeProfile.Create(party.Id)); report.EmployeeProfilesCreated++; }
            }

            // ── CreditLimit → CreditAccount + AsignacionInicial ────────────────
            if (party.CreditLimit is > 0m)
            {
                if (customerProfileId is null)
                {
                    // Anomalía: crédito sin faceta cliente. No se inventa CustomerProfile — se reporta.
                    report.CreditWithoutCustomer.Add(party.Id);
                }
                else
                {
                    var hasAccount = await db.CreditAccounts.AsNoTracking()
                        .AnyAsync(a => a.CustomerProfileId == customerProfileId.Value, ct).ConfigureAwait(false);
                    if (hasAccount)
                    {
                        report.CreditAccountsSkipped++;
                    }
                    else
                    {
                        var account = CreditAccount.Open(
                            customerProfileId.Value, tenantId, party.CreditLimit.Value, "backfill-v1",
                            monedaId: string.IsNullOrWhiteSpace(party.CreditCurrency) ? "COP" : party.CreditCurrency!,
                            diasCredito: int.TryParse(party.CreditDaysCode, out var d) ? d : 0);
                        if (!dryRun) db.CreditAccounts.Add(account);
                        report.CreditAccountsCreated++;
                    }
                }
            }

            // ── CreditBlocked → PartyHold(Ventas) ──────────────────────────────
            if (party.CreditBlocked)
            {
                var hasHold = await db.PartyHolds.AsNoTracking()
                    .AnyAsync(h => h.PartyId == party.Id && h.HoldType == HoldType.Ventas && h.EstaActivo, ct).ConfigureAwait(false);
                if (hasHold)
                {
                    report.HoldsSkipped++;
                }
                else
                {
                    // NOTA(PR-D): revisar la semántica exacta. CreditBlocked en v1/Effi probablemente
                    // significaba "no a crédito", no "no vende". HoldType.Ventas es la aproximación
                    // reversible por ahora; PR-D puede reclasificar a un hold de crédito específico.
                    var hold = PartyHold.Place(
                        party.Id, HoldType.Ventas,
                        "Backfill v1: CreditBlocked=true (crédito bloqueado en Effi/v1)",
                        today, "backfill-v1");
                    if (!dryRun) db.PartyHolds.Add(hold);
                    report.HoldsCreated++;
                }
            }

            // ── CIIU string → PartyCiiuActivity(principal) ─────────────────────
            if (!string.IsNullOrWhiteSpace(party.ActividadEconomicaCiiuCode))
            {
                var exists = await db.PartyCiiuActivities.AsNoTracking()
                    .AnyAsync(c => c.PartyId == party.Id, ct).ConfigureAwait(false);
                if (exists) report.CiiuActivitiesSkipped++;
                else
                {
                    if (!dryRun) db.PartyCiiuActivities.Add(
                        PartyCiiuActivity.Create(party.Id, party.ActividadEconomicaCiiuCode!, isPrincipal: true));
                    report.CiiuActivitiesCreated++;
                }
            }

            // ── TaxRegimeCode → FiscalData (PR-D1: ahora PERSISTE; cierra el gap analiza-only de PR-C) ──
            var fiscal = TaxRegimeMapper.Map(party.TaxRegimeCode);
            if (fiscal.IsEmpty)
            {
                report.TaxRegimeEmpty++;
            }
            else if (fiscal.Mapped)
            {
                report.TaxRegimeMappedClean++;

                // Idempotente y robusto a la materialización EF (null vs VO vacío): solo poblar si
                // los dos ejes están sin asignar. AssignFiscalData reemplaza el VO (en backfill
                // FiscalData arranca vacío, no hay flags que preservar).
                bool fiscalEmpty = party.FiscalData is null
                    || (party.FiscalData.RegimenTributario is null && party.FiscalData.ResponsabilidadIVA is null);
                if (fiscalEmpty && (fiscal.Regimen is not null || fiscal.ResponsabilidadIVA is not null))
                {
                    if (!dryRun)
                    {
                        party.AssignFiscalData(new FiscalData
                        {
                            RegimenTributario = fiscal.Regimen,
                            ResponsabilidadIVA = fiscal.ResponsabilidadIVA,
                        });
                    }
                    report.FiscalDataPopulated++;
                }
            }
            else
            {
                report.TaxRegimeUnmapped.Add(party.Id);
            }
        }

        if (!dryRun)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Parties v2 backfill ({Mode}) tenant {TenantId}: scanned {Scanned}, " +
                "customer +{Cust}, supplier +{Supp}, employee +{Emp}, credit +{Credit}, ciiu +{Ciiu}, holds +{Holds}, " +
                "taxregime-unmapped {Unmapped}, credit-without-customer {Anom}",
                dryRun ? "dry-run" : "apply", tenantId, report.PartiesScanned,
                report.CustomerProfilesCreated, report.SupplierProfilesCreated, report.EmployeeProfilesCreated,
                report.CreditAccountsCreated, report.CiiuActivitiesCreated, report.HoldsCreated,
                report.TaxRegimeUnmapped.Count, report.CreditWithoutCustomer.Count);
        }

        return report;
    }
}
