using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Data;
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

    [Fact]
    public async Task CreditAccount_With_Movements_Persists_And_Reloads()
    {
        var root = Tenant(TestConstants.RootTenantId);
        var profileId = Guid.CreateVersion7();
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
        var partyId = Guid.CreateVersion7();

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
        var partyId = Guid.CreateVersion7();
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
    public async Task New_Tables_Are_Tenant_Isolated()
    {
        var tenantA = Tenant($"iso-a-{Guid.NewGuid():N}"[..16]);
        var tenantB = Tenant($"iso-b-{Guid.NewGuid():N}"[..16]);
        var partyA = Guid.CreateVersion7();

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
