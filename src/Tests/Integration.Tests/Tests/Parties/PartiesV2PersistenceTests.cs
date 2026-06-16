using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Credit;
using FSH.Modules.Parties.Domain.V2.Profiles;
using Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-B (SPEC parties §0.1) — persistencia v2 ADITIVA. Verifica que las tablas nuevas
/// (Profiles, CreditAccounts, etc.) persisten y recargan, que los índices del SPEC §15 se
/// comportan, que el primer <c>OwnsOne</c> del repo (PaymentTerms) round-trips, y que el
/// aislamiento por tenant (shadow TenantId de Finbuckle) aplica a las tablas nuevas.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2PersistenceTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2PersistenceTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Tenant(string id) => new(id, id);

    private async Task WithTenant(AppTenantInfo tenant, Func<PartiesDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        var db = scope.ServiceProvider.GetRequiredService<PartiesDbContext>();
        await action(db);
    }

    // PR-D2: con las FK Profile→Party / CIIU→Party / CreditAccount→CustomerProfile, las filas v2
    // ya no pueden ser huérfanas. Estos helpers siembran las filas padre reales primero.
    private async Task<Guid> SeedPartyAsync(AppTenantInfo tenant)
    {
        Guid id = Guid.Empty;
        await WithTenant(tenant, async db =>
        {
            var party = Party.Create("NIT", $"seed-{Guid.NewGuid():N}"[..14], 1,
                PartyKind.Juridica, "Seed Party", PartyRole.Customer);
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            id = party.Id;
        });
        return id;
    }

    private async Task<Guid> SeedCustomerProfileAsync(AppTenantInfo tenant, Guid partyId)
    {
        Guid id = Guid.Empty;
        await WithTenant(tenant, async db =>
        {
            var profile = CustomerProfile.Create(partyId);
            db.CustomerProfiles.Add(profile);
            await db.SaveChangesAsync();
            id = profile.Id;
        });
        return id;
    }

    [Fact]
    public async Task CreditAccount_With_Movements_Persists_And_Reloads()
    {
        var root = Tenant(TestConstants.RootTenantId);
        var partyId = await SeedPartyAsync(root);
        var profileId = await SeedCustomerProfileAsync(root, partyId);
        Guid accountId = Guid.Empty;

        await WithTenant(root, async db =>
        {
            var account = CreditAccount.Open(profileId, root.Id, cupoInicial: 1_000_000m, registradoPor: "tester");
            account.AddMovement(CreditMovementType.Consumo, 250_000m, "tester");
            account.AddMovement(CreditMovementType.Liberacion, 100_000m, "tester");
            db.CreditAccounts.Add(account);
            await db.SaveChangesAsync();
            accountId = account.Id;
        });

        // Reload en un scope NUEVO (sin identity-map) — el historial y el saldo deben sobrevivir.
        await WithTenant(root, async db =>
        {
            var reloaded = await db.CreditAccounts
                .Include(a => a.Movements)
                .SingleAsync(a => a.Id == accountId);

            reloaded.CupoAsignado.ShouldBe(1_000_000m);
            reloaded.SaldoDisponible.ShouldBe(850_000m); // 1.000.000 − 250.000 + 100.000
            reloaded.Movements.Count.ShouldBe(3);
            reloaded.Movements.ShouldContain(m => m.Tipo == CreditMovementType.AsignacionInicial);
        });
    }

    [Fact]
    public async Task Ix_Ciiu_Principal_Prevents_Two_Principals_For_Same_Party()
    {
        var root = Tenant(TestConstants.RootTenantId);
        var partyId = await SeedPartyAsync(root);

        await WithTenant(root, async db =>
        {
            db.PartyCiiuActivities.Add(PartyCiiuActivity.Create(partyId, "4791", isPrincipal: true));
            await db.SaveChangesAsync();
        });

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithTenant(root, async db =>
            {
                db.PartyCiiuActivities.Add(PartyCiiuActivity.Create(partyId, "6201", isPrincipal: true));
                await db.SaveChangesAsync(); // viola ix_ciiu_principal (único parcial)
            });
        });
    }

    [Fact]
    public async Task Ix_Holds_Active_Returns_Only_Active_Holds()
    {
        var root = Tenant(TestConstants.RootTenantId);
        var partyId = Guid.CreateVersion7();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await WithTenant(root, async db =>
        {
            var active = PartyHold.Place(partyId, HoldType.Ventas, "cartera vencida", today, "tester");
            var released = PartyHold.Place(partyId, HoldType.Compras, "histórico", today, "tester");
            released.Release();
            db.PartyHolds.AddRange(active, released);
            await db.SaveChangesAsync();
        });

        await WithTenant(root, async db =>
        {
            var activos = await db.PartyHolds
                .Where(h => h.PartyId == partyId && h.EstaActivo)
                .ToListAsync();

            activos.Count.ShouldBe(1);
            activos[0].HoldType.ShouldBe(HoldType.Ventas);
        });
    }

    [Fact]
    public async Task SupplierProfile_Owned_PaymentTerms_RoundTrips()
    {
        var root = Tenant(TestConstants.RootTenantId);
        var partyId = await SeedPartyAsync(root);
        var currencyId = Guid.CreateVersion7();
        var formaPagoId = Guid.CreateVersion7();
        Guid profileId = Guid.Empty;

        await WithTenant(root, async db =>
        {
            var supplier = SupplierProfile.Create(
                partyId, currencyId,
                paymentTerms: new PaymentTerms { DiasCredito = 30, FormaPagoId = formaPagoId });
            db.SupplierProfiles.Add(supplier);
            await db.SaveChangesAsync();
            profileId = supplier.Id;
        });

        await WithTenant(root, async db =>
        {
            var reloaded = await db.SupplierProfiles.SingleAsync(s => s.Id == profileId);
            reloaded.PaymentTerms.ShouldNotBeNull();
            reloaded.PaymentTerms.DiasCredito.ShouldBe(30);
            reloaded.PaymentTerms.FormaPagoId.ShouldBe(formaPagoId);
        });
    }

    [Fact]
    public async Task LegalRepresentative_Owned_Nullable_RoundTrips()
    {
        // PR-D4: owned nullable. Party sin rep → null; Party con rep → round-trip incl. enum.
        var root = Tenant(TestConstants.RootTenantId);

        var sinRepId = await SeedPartyAsync(root);
        await WithTenant(root, async db =>
        {
            var p = await db.Parties.SingleAsync(x => x.Id == sinRepId);
            p.LegalRepresentative.ShouldBeNull();
        });

        var conRepId = await SeedPartyAsync(root);
        await WithTenant(root, async db =>
        {
            var p = await db.Parties.SingleAsync(x => x.Id == conRepId);
            p.AssignLegalRepresentative(FSH.Modules.Parties.Domain.V2.LegalRepresentative.Create(
                "Ana", "Gómez", TipoIdentificacion.CC, "52000111", celular: "3001234567", esPEP: true));
            await db.SaveChangesAsync();
        });

        await WithTenant(root, async db =>
        {
            var p = await db.Parties.SingleAsync(x => x.Id == conRepId);
            p.LegalRepresentative.ShouldNotBeNull();
            p.LegalRepresentative!.Nombres.ShouldBe("Ana");
            p.LegalRepresentative.TipoIdentificacion.ShouldBe(TipoIdentificacion.CC);
            p.LegalRepresentative.NumeroIdentificacion.ShouldBe("52000111");
            p.LegalRepresentative.EsPEP.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Party_Navigates_To_Customer_And_Supplier_Profiles()
    {
        // PR-D2: navs one-to-one Party→Profiles. Insertamos Party + 2 facetas y navegamos vía Include.
        var root = Tenant(TestConstants.RootTenantId);
        Guid partyId = Guid.Empty;

        await WithTenant(root, async db =>
        {
            var party = FSH.Modules.Parties.Domain.Party.Create(
                "NIT", $"nav-{Guid.NewGuid():N}"[..14], 1, FSH.Modules.Parties.Contracts.Enums.PartyKind.Juridica,
                "Tercero Navegable", FSH.Modules.Parties.Contracts.Enums.PartyRole.Customer | FSH.Modules.Parties.Contracts.Enums.PartyRole.Supplier);
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            partyId = party.Id;

            db.CustomerProfiles.Add(CustomerProfile.Create(partyId));
            db.SupplierProfiles.Add(SupplierProfile.Create(partyId));
            await db.SaveChangesAsync();
        });

        await WithTenant(root, async db =>
        {
            var party = await db.Parties
                .Include(p => p.CustomerProfile)
                .Include(p => p.SupplierProfile)
                .SingleAsync(p => p.Id == partyId);
            party.CustomerProfile.ShouldNotBeNull();
            party.SupplierProfile.ShouldNotBeNull();
            party.SupplierProfile!.PartyId.ShouldBe(partyId);
        });
    }

    [Fact]
    public async Task CustomerProfile_With_CreditAccount_Cannot_Be_Hard_Deleted()
    {
        // PR-D2 (cascade verificación, opción "b"): FK Restrict CreditAccount→CustomerProfile.
        // Un cliente con historial de crédito NO puede borrarse (protege el log de movimientos).
        var root = Tenant(TestConstants.RootTenantId);
        Guid profileId = Guid.Empty;

        await WithTenant(root, async db =>
        {
            var party = FSH.Modules.Parties.Domain.Party.Create(
                "NIT", $"del-{Guid.NewGuid():N}"[..14], 1, FSH.Modules.Parties.Contracts.Enums.PartyKind.Juridica,
                "Cliente Con Crédito", FSH.Modules.Parties.Contracts.Enums.PartyRole.Customer);
            db.Parties.Add(party);
            await db.SaveChangesAsync();

            var profile = CustomerProfile.Create(party.Id);
            db.CustomerProfiles.Add(profile);
            await db.SaveChangesAsync();
            profileId = profile.Id;

            db.CreditAccounts.Add(CreditAccount.Open(profileId, root.Id, 500_000m, "tester"));
            await db.SaveChangesAsync();
        });

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithTenant(root, async db =>
            {
                var profile = await db.CustomerProfiles.SingleAsync(p => p.Id == profileId);
                db.CustomerProfiles.Remove(profile);
                await db.SaveChangesAsync(); // Restrict de CreditAccount bloquea
            });
        });
    }

    [Fact]
    public async Task New_Tables_Are_Tenant_Isolated()
    {
        var tenantA = Tenant($"iso-a-{Guid.NewGuid():N}"[..16]);
        var tenantB = Tenant($"iso-b-{Guid.NewGuid():N}"[..16]);
        var partyA = await SeedPartyAsync(tenantA);

        await WithTenant(tenantA, async db =>
        {
            db.CustomerProfiles.Add(CustomerProfile.Create(partyA));
            await db.SaveChangesAsync();
        });

        // Tenant B no debe ver la fila de A (shadow TenantId + global query filter).
        await WithTenant(tenantB, async db =>
        {
            var visibleFromB = await db.CustomerProfiles.AnyAsync(p => p.PartyId == partyA);
            visibleFromB.ShouldBeFalse();
        });

        // Tenant A sí la ve.
        await WithTenant(tenantA, async db =>
        {
            var visibleFromA = await db.CustomerProfiles.AnyAsync(p => p.PartyId == partyA);
            visibleFromA.ShouldBeTrue();
        });
    }
}
