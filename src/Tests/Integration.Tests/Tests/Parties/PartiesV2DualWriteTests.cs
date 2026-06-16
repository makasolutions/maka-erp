using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.Credit;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-D5a — dual-write Roles→Profiles a través de los handlers reales Create/Update (valida la
/// danza de change-tracking del Update con los profiles v2). Cada operación corre en su propio
/// scope (DbContext fresco, como en producción).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2DualWriteTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2DualWriteTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Root => new(TestConstants.RootTenantId, TestConstants.RootTenantId);

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Root);
        return await action(scope.ServiceProvider);
    }

    private Task<Guid> CreateAsync(string num, PartyRole roles, decimal? creditLimit = null, bool creditBlocked = false,
        string? taxRegimeCode = null, string? ciiu = null) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Tercero {num}", roles,
                TaxRegimeCode: taxRegimeCode, CreditLimit: creditLimit,
                ActividadEconomicaCiiuCode: ciiu, CreditBlocked: creditBlocked), default).AsTask());

    private Task<Guid> UpdateRolesAsync(Guid id, PartyRole roles, decimal? creditLimit = null, bool creditBlocked = false,
        string? taxRegimeCode = null, string? ciiu = null) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<UpdatePartyCommand, Guid>>()
            .Handle(new UpdatePartyCommand(id, null, PartyKind.Juridica, "Tercero", roles,
                null, null, null, taxRegimeCode, null, PartyStatus.Active, LifecycleStage.Lead, 0,
                null, null, null, null, null, null, creditLimit, null, null, null,
                ActividadEconomicaCiiuCode: ciiu, CreditBlocked: creditBlocked), default).AsTask());

    private Task<(bool exists, decimal cupo, bool activo, int movimientos, int aumentos, int reducciones)> ReadCreditAsync(Guid partyId) =>
        InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var cp = await db.CustomerProfiles.FirstOrDefaultAsync(p => p.PartyId == partyId);
            if (cp is null) return (false, 0m, false, 0, 0, 0);
            var acc = await db.CreditAccounts.Include(a => a.Movements).FirstOrDefaultAsync(a => a.CustomerProfileId == cp.Id);
            if (acc is null) return (false, 0m, false, 0, 0, 0);
            return (true, acc.CupoAsignado, acc.EstaActivo, acc.Movements.Count,
                acc.Movements.Count(m => m.Tipo == CreditMovementType.Aumento),
                acc.Movements.Count(m => m.Tipo == CreditMovementType.Reduccion));
        });

    private Task<(bool custExists, bool? custActive, bool suppExists, bool? suppActive, int custCount, int suppCount)> ReadAsync(Guid id) =>
        InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var cust = await db.CustomerProfiles.Where(p => p.PartyId == id).ToListAsync();
            var supp = await db.SupplierProfiles.Where(p => p.PartyId == id).ToListAsync();
            return (cust.Count > 0, cust.FirstOrDefault()?.IsActive, supp.Count > 0, supp.FirstOrDefault()?.IsActive,
                    cust.Count, supp.Count);
        });

    [Fact]
    public async Task Create_With_Customer_And_Supplier_Creates_Two_Active_Profiles()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer | PartyRole.Supplier);

        var s = await ReadAsync(id);
        s.custExists.ShouldBeTrue();
        s.custActive.ShouldBe(true);
        s.suppExists.ShouldBeTrue();
        s.suppActive.ShouldBe(true);
    }

    [Fact]
    public async Task Update_Removing_Supplier_Deactivates_It_Keeping_Customer()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer | PartyRole.Supplier);

        await UpdateRolesAsync(id, PartyRole.Customer); // quita Supplier

        var s = await ReadAsync(id);
        s.custActive.ShouldBe(true);          // Customer sigue activo
        s.suppExists.ShouldBeTrue();          // NO se borró
        s.suppActive.ShouldBe(false);         // soft-desactivado (preserva historial)
        s.suppCount.ShouldBe(1);
    }

    [Fact]
    public async Task Resaving_Unchanged_Does_Not_Duplicate_Profiles()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer | PartyRole.Supplier);

        await UpdateRolesAsync(id, PartyRole.Customer | PartyRole.Supplier); // mismos roles
        await UpdateRolesAsync(id, PartyRole.Customer | PartyRole.Supplier); // otra vez

        var s = await ReadAsync(id);
        s.custCount.ShouldBe(1);              // sin duplicados
        s.suppCount.ShouldBe(1);
        s.custActive.ShouldBe(true);
        s.suppActive.ShouldBe(true);
    }

    [Fact]
    public async Task Update_Readding_Supplier_Reactivates_Same_Profile()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer | PartyRole.Supplier);

        await UpdateRolesAsync(id, PartyRole.Customer);                       // quita Supplier (soft off)
        await UpdateRolesAsync(id, PartyRole.Customer | PartyRole.Supplier);  // lo vuelve a poner

        var s = await ReadAsync(id);
        s.suppCount.ShouldBe(1);              // reactivó el mismo, no creó un segundo
        s.suppActive.ShouldBe(true);
    }

    // ── Crédito (D5b) ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_CreditLimit_Positive_Opens_Account_With_Initial_Movement()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 1_000_000m);

        var c = await ReadCreditAsync(id);
        c.exists.ShouldBeTrue();
        c.cupo.ShouldBe(1_000_000m);
        c.activo.ShouldBeTrue();
        c.movimientos.ShouldBe(1); // AsignacionInicial
    }

    [Fact]
    public async Task Create_CreditLimit_Zero_Opens_No_Account()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 0m);
        (await ReadCreditAsync(id)).exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Update_Increase_Cupo_Adds_One_Aumento()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 500_000m);
        await UpdateRolesAsync(id, PartyRole.Customer, creditLimit: 800_000m);

        var c = await ReadCreditAsync(id);
        c.cupo.ShouldBe(800_000m);
        c.aumentos.ShouldBe(1);
        c.movimientos.ShouldBe(2); // AsignacionInicial + Aumento
    }

    [Fact]
    public async Task Update_Decrease_Cupo_Adds_One_Reduccion()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 500_000m);
        await UpdateRolesAsync(id, PartyRole.Customer, creditLimit: 300_000m);

        var c = await ReadCreditAsync(id);
        c.cupo.ShouldBe(300_000m);
        c.reducciones.ShouldBe(1);
        c.movimientos.ShouldBe(2);
    }

    [Fact]
    public async Task Update_Same_Cupo_Adds_Zero_Movements()  // CRUX de idempotencia
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 500_000m);

        await UpdateRolesAsync(id, PartyRole.Customer, creditLimit: 500_000m); // mismo cupo
        await UpdateRolesAsync(id, PartyRole.Customer, creditLimit: 500_000m); // otra vez

        var c = await ReadCreditAsync(id);
        c.cupo.ShouldBe(500_000m);
        c.movimientos.ShouldBe(1);  // SOLO el AsignacionInicial — cero movimientos espurios
        c.aumentos.ShouldBe(0);
        c.reducciones.ShouldBe(0);
    }

    [Fact]
    public async Task Update_CreditLimit_To_Zero_Reduces_To_Zero_And_Deactivates()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditLimit: 500_000m);
        await UpdateRolesAsync(id, PartyRole.Customer, creditLimit: 0m);

        var c = await ReadCreditAsync(id);
        c.cupo.ShouldBe(0m);
        c.activo.ShouldBeFalse();      // desactivado
        c.reducciones.ShouldBe(1);     // historial intacto: AsignacionInicial + Reduccion a 0
        c.movimientos.ShouldBe(2);
    }

    [Fact]
    public async Task CreditLimit_Without_Customer_Role_Skips_Credit()
    {
        // Rol Supplier (no Customer) + CreditLimit>0 → anomalía → no se crea crédito.
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Supplier, creditLimit: 999_000m);
        (await ReadCreditAsync(id)).exists.ShouldBeFalse();
    }

    [Fact]
    public async Task CreditBlocked_Creates_Ventas_Hold_Idempotently()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, creditBlocked: true);

        await UpdateRolesAsync(id, PartyRole.Customer, creditBlocked: true); // re-save: no debe duplicar

        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var holds = await db.PartyHolds.Where(h => h.PartyId == id && h.HoldType == HoldType.Ventas).ToListAsync();
            holds.Count.ShouldBe(1);          // idempotente
            holds[0].EstaActivo.ShouldBeTrue();
            return 0;
        });
    }

    // ── FiscalData + CIIU (D5c) ────────────────────────────────────────────────────

    private Task<FiscalData?> ReadFiscalAsync(Guid id) =>
        InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            return (await db.Parties.SingleAsync(p => p.Id == id)).FiscalData;
        });

    [Fact]
    public async Task Create_Mappable_TaxRegime_Populates_Fiscal_Axes()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer,
            taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA");

        var f = await ReadFiscalAsync(id);
        f.ShouldNotBeNull();
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
    }

    [Fact]
    public async Task Unmappable_TaxRegime_Leaves_Fiscal_Untouched()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer,
            taxRegimeCode: "CODIGO_INVENTADO_XYZ");

        var f = await ReadFiscalAsync(id);
        f?.RegimenTributario.ShouldBeNull(); // no se derivó nada
    }

    [Fact]
    public async Task Update_TaxRegime_Updates_Axes_Preserving_Other_Fields()
    {
        // EL TEST IMPORTANTE: el dual-write deriva solo los ejes; NO debe pisar GranContribuyente
        // (campo que v1 no conoce, poblado por la vía v2).
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, taxRegimeCode: "SIMPLE");

        // Pre-pueblo GranContribuyente=true + Regimen=Simple por la vía v2 (simula otra fuente).
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var p = await db.Parties.SingleAsync(x => x.Id == id);
            p.AssignFiscalData(new FiscalData
            {
                RegimenTributario = RegimenTributario.Simple,
                ResponsabilidadIVA = ResponsabilidadIVA.NoResponsable,
                GranContribuyente = true,
                ResponsabilidadesFiscales = ["O-13", "O-15"],
            });
            await db.SaveChangesAsync();
            return 0;
        });

        // Update cambia el régimen v1 a común → ejes nuevos, pero GranContribuyente + responsabilidades preservados.
        await UpdateRolesAsync(id, PartyRole.Customer, taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA");

        var f = await ReadFiscalAsync(id);
        f.ShouldNotBeNull();
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);   // eje actualizado
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable); // eje actualizado
        f.GranContribuyente.ShouldBeTrue();                            // PRESERVADO (v1 no lo conoce)
        f.ResponsabilidadesFiscales.ShouldBe(new[] { "O-13", "O-15" }); // PRESERVADO
    }

    [Fact]
    public async Task Update_Same_TaxRegime_Preserves_Fiscal()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, taxRegimeCode: "SIMPLE");
        await UpdateRolesAsync(id, PartyRole.Customer, taxRegimeCode: "SIMPLE"); // mismo régimen → no-op

        var f = await ReadFiscalAsync(id);
        f!.RegimenTributario.ShouldBe(RegimenTributario.Simple);
    }

    [Fact]
    public async Task Create_With_Ciiu_Creates_Principal_Activity()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, ciiu: "4791");

        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var acts = await db.PartyCiiuActivities.Where(c => c.PartyId == id).ToListAsync();
            acts.Count.ShouldBe(1);
            acts[0].CiiuCode.ShouldBe("4791");
            acts[0].IsPrincipal.ShouldBeTrue();
            return 0;
        });
    }

    [Fact]
    public async Task Update_Ciiu_Updates_Code_In_Place()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, ciiu: "4791");
        await UpdateRolesAsync(id, PartyRole.Customer, ciiu: "6201"); // cambia el código

        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var acts = await db.PartyCiiuActivities.Where(c => c.PartyId == id).ToListAsync();
            acts.Count.ShouldBe(1);             // in-place: sigue siendo UNA actividad
            acts[0].CiiuCode.ShouldBe("6201");  // código nuevo
            acts[0].IsPrincipal.ShouldBeTrue(); // sin swap del flag → no violó ix_ciiu_principal
            return 0;
        });
    }

    [Fact]
    public async Task Update_Same_Ciiu_NoOp()
    {
        var id = await CreateAsync($"d5-{Guid.NewGuid():N}"[..13], PartyRole.Customer, ciiu: "4791");
        await UpdateRolesAsync(id, PartyRole.Customer, ciiu: "4791"); // mismo código

        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            (await db.PartyCiiuActivities.CountAsync(c => c.PartyId == id)).ShouldBe(1);
            return 0;
        });
    }
}
