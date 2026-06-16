using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Data;
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

    private Task<Guid> CreateAsync(string num, PartyRole roles) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Tercero {num}", roles), default).AsTask());

    private Task<Guid> UpdateRolesAsync(Guid id, PartyRole roles) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<UpdatePartyCommand, Guid>>()
            .Handle(new UpdatePartyCommand(id, null, PartyKind.Juridica, "Tercero", roles,
                null, null, null, null, null, PartyStatus.Active, LifecycleStage.Lead, 0,
                null, null, null, null, null, null, null, null, null, null), default).AsTask());

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
}
