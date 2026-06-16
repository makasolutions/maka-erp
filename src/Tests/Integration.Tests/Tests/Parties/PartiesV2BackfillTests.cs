using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2.Credit;
using FSH.Modules.Parties.Migration;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-C (SPEC parties §0.1) — backfill v1→v2. Verifica los mapeos (Roles→Profiles,
/// CreditLimit→CreditAccount, CIIU→PartyCiiuActivity, CreditBlocked→Hold), la idempotencia
/// (correr 2 veces = mismo estado), el análisis sin-persistencia de TaxRegimeCode, y el dry-run.
/// Cada test usa un tenant fabricado aislado para no chocar con otros.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2BackfillTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2BackfillTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Tenant() => new($"bf-{Guid.NewGuid():N}"[..16], $"bf-{Guid.NewGuid():N}"[..16]);

    private async Task<T> InTenant<T>(AppTenantInfo tenant, Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        return await action(scope.ServiceProvider);
    }

    private async Task SeedPartyAsync(AppTenantInfo tenant, Party party) =>
        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            return 0;
        });

    private Task<BackfillReport> RunBackfillAsync(AppTenantInfo tenant, bool dryRun) =>
        InTenant(tenant, sp =>
            sp.GetRequiredService<PartiesV2BackfillService>().RunAsync(dryRun, tenant.Id, CancellationToken.None));

    [Fact]
    public async Task Backfill_Is_Idempotent()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("CC", "111", null, PartyKind.Natural, "Cliente Uno",
            PartyRole.Customer, actividadEconomicaCiiuCode: "4791", creditLimit: 500_000m));

        var first = await RunBackfillAsync(tenant, dryRun: false);
        var second = await RunBackfillAsync(tenant, dryRun: false);

        first.CustomerProfilesCreated.ShouldBe(1);
        first.CreditAccountsCreated.ShouldBe(1);
        first.CiiuActivitiesCreated.ShouldBe(1);

        // Segunda corrida: nada nuevo creado, las filas existentes se saltan.
        second.CustomerProfilesCreated.ShouldBe(0);
        second.CustomerProfilesSkipped.ShouldBe(1);
        second.CreditAccountsCreated.ShouldBe(0);
        second.CreditAccountsSkipped.ShouldBe(1);
        second.CiiuActivitiesCreated.ShouldBe(0);
        second.CiiuActivitiesSkipped.ShouldBe(1);

        // Estado final: exactamente una fila por tabla.
        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            (await db.CustomerProfiles.CountAsync()).ShouldBe(1);
            (await db.CreditAccounts.CountAsync()).ShouldBe(1);
            (await db.PartyCiiuActivities.CountAsync()).ShouldBe(1);
            return 0;
        });
    }

    [Fact]
    public async Task Customer_And_Supplier_Roles_Create_Two_Profiles()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("NIT", "900", 1, PartyKind.Juridica, "Empresa Dual",
            PartyRole.Customer | PartyRole.Supplier));

        var report = await RunBackfillAsync(tenant, dryRun: false);

        report.CustomerProfilesCreated.ShouldBe(1);
        report.SupplierProfilesCreated.ShouldBe(1);
        report.SuppliersWithoutCurrency.ShouldBe(1); // sin catálogo de monedas (PR-D)
    }

    [Fact]
    public async Task CreditLimit_Positive_Creates_Account_With_Initial_Movement()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("CC", "222", null, PartyKind.Natural, "Con Crédito",
            PartyRole.Customer, creditLimit: 1_500_000m));

        await RunBackfillAsync(tenant, dryRun: false);

        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var account = await db.CreditAccounts.Include(a => a.Movements).SingleAsync();
            account.CupoAsignado.ShouldBe(1_500_000m);
            account.SaldoDisponible.ShouldBe(1_500_000m);
            account.Movements.Count.ShouldBe(1);
            account.Movements[0].Tipo.ShouldBe(CreditMovementType.AsignacionInicial);
            return 0;
        });
    }

    [Fact]
    public async Task CreditLimit_Zero_Creates_No_Account()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("CC", "333", null, PartyKind.Natural, "Sin Crédito",
            PartyRole.Customer, creditLimit: 0m));

        var report = await RunBackfillAsync(tenant, dryRun: false);

        report.CustomerProfilesCreated.ShouldBe(1);
        report.CreditAccountsCreated.ShouldBe(0);
        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            (await db.CreditAccounts.CountAsync()).ShouldBe(0);
            return 0;
        });
    }

    [Fact]
    public async Task Unmappable_TaxRegimeCode_Is_Reported_Not_Thrown()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("NIT", "444", 1, PartyKind.Juridica, "Régimen Raro",
            PartyRole.Customer, taxRegimeCode: "CODIGO_INVENTADO_XYZ"));

        var report = await RunBackfillAsync(tenant, dryRun: false);

        // No lanza; el código no derivable queda reportado como deuda PR-D, y NO se puebla FiscalData.
        report.TaxRegimeUnmapped.ShouldContain(report.TaxRegimeUnmapped.Single());
        report.TaxRegimeMappedClean.ShouldBe(0);
        report.FiscalDataPopulated.ShouldBe(0);

        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.SingleAsync(p => p.IdentificationNumber == "444");
            party.FiscalData?.RegimenTributario.ShouldBeNull();
            return 0;
        });
    }

    [Fact]
    public async Task Mappable_TaxRegimeCode_Persists_FiscalData_Idempotently()
    {
        // PR-D1: el backfill ahora PERSISTE el régimen en Party.FiscalData (cierra el gap
        // analiza-only de PR-C). "REGIMEN_COMUN_RESPONSABLE_IVA" → Ordinario + Responsable.
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("NIT", "777", 1, PartyKind.Juridica, "Régimen Común",
            PartyRole.Customer, taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA"));

        var first = await RunBackfillAsync(tenant, dryRun: false);
        first.FiscalDataPopulated.ShouldBe(1);
        first.TaxRegimeMappedClean.ShouldBe(1);

        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.SingleAsync(p => p.IdentificationNumber == "777");
            party.FiscalData.ShouldNotBeNull();
            party.FiscalData!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
            party.FiscalData.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
            return 0;
        });

        // Idempotente: segunda corrida no vuelve a poblar.
        var second = await RunBackfillAsync(tenant, dryRun: false);
        second.FiscalDataPopulated.ShouldBe(0);
    }

    [Fact]
    public async Task FiscalData_ResponsabilidadesFiscales_ValueConverter_RoundTrips()
    {
        // El value converter de la colección DIAN (códigos R-99-PN/O-13…) round-trips inline.
        var tenant = Tenant();
        var partyId = Guid.Empty;
        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = Party.Create("NIT", "888", 1, PartyKind.Juridica, "Con Responsabilidades", PartyRole.Customer);
            party.AssignFiscalData(new FSH.Modules.Parties.Domain.V2.FiscalData
            {
                RegimenTributario = RegimenTributario.Ordinario,
                ResponsabilidadesFiscales = ["R-99-PN", "O-13", "O-15"],
                GranContribuyente = true,
            });
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            partyId = party.Id;
            return 0;
        });

        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.SingleAsync(p => p.Id == partyId);
            party.FiscalData.ShouldNotBeNull();
            party.FiscalData!.ResponsabilidadesFiscales.ShouldBe(new[] { "R-99-PN", "O-13", "O-15" });
            party.FiscalData.GranContribuyente.ShouldBeTrue();
            return 0;
        });
    }

    [Fact]
    public async Task DryRun_Writes_Nothing()
    {
        var tenant = Tenant();
        await SeedPartyAsync(tenant, Party.Create("CC", "555", null, PartyKind.Natural, "Dry Run",
            PartyRole.Customer, actividadEconomicaCiiuCode: "6201", creditLimit: 300_000m));

        var report = await RunBackfillAsync(tenant, dryRun: true);

        // El reporte cuenta lo que HARÍA…
        report.CustomerProfilesCreated.ShouldBe(1);
        report.CreditAccountsCreated.ShouldBe(1);
        report.CiiuActivitiesCreated.ShouldBe(1);

        // …pero nada se escribió.
        await InTenant(tenant, async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            (await db.CustomerProfiles.CountAsync()).ShouldBe(0);
            (await db.CreditAccounts.CountAsync()).ShouldBe(0);
            (await db.PartyCiiuActivities.CountAsync()).ShouldBe(0);
            return 0;
        });
    }
}
