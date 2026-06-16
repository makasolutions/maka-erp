using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// Front fiscal v2 — el comando acepta los EJES fiscales directamente (PartyFiscalAxesInput),
/// camino autoritativo que además es el ÚNICO que escribe ResponsabilidadesFiscales (la lista no
/// tenía write-path tras F1b). Verifica persistencia, round-trip de la lista, la guarda anti-wipe,
/// y que el modo legacy (TaxRegimeCode) sigue funcionando.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2FiscalAxesTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2FiscalAxesTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Root => new(TestConstants.RootTenantId, TestConstants.RootTenantId);

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Root);
        return await action(scope.ServiceProvider);
    }

    private Task<Guid> CreateAsync(string num, PartyFiscalAxesInput? axes = null, string? taxRegimeCode = null) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Tercero {num}", PartyRole.Customer,
                TaxRegimeCode: taxRegimeCode, FiscalAxes: axes), default).AsTask());

    private Task<Guid> UpdateAsync(Guid id, PartyFiscalAxesInput? axes) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<UpdatePartyCommand, Guid>>()
            .Handle(new UpdatePartyCommand(id, null, PartyKind.Juridica, "Tercero", PartyRole.Customer,
                null, null, null, null, null, PartyStatus.Active, LifecycleStage.Lead, 0,
                null, null, null, null, null, null, null, null, null, null,
                FiscalAxes: axes), default).AsTask());

    private Task<PartyV2FiscalDto?> ReadFiscalAsync(Guid id) =>
        InScope(async sp => (await sp.GetRequiredService<IQueryHandler<GetPartyByIdQuery, PartyDetailDto>>()
            .Handle(new GetPartyByIdQuery(id), default)).V2?.Fiscal);

    [Fact]
    public async Task Create_With_FiscalAxes_Persists_Axes_And_List()
    {
        var num = $"fa-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, new PartyFiscalAxesInput(
            RegimenTributario.Ordinario, ResponsabilidadIVA.Responsable, ["O-13", "O-15"]));

        var f = await ReadFiscalAsync(id);
        f.ShouldNotBeNull();
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
        f.ResponsabilidadesFiscales.ShouldBe(new[] { "O-13", "O-15" }); // ÚNICO write-path de la lista
    }

    [Fact]
    public async Task Update_FiscalAxes_Changes_Regimen_And_List()
    {
        var num = $"fa-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, new PartyFiscalAxesInput(
            RegimenTributario.Ordinario, ResponsabilidadIVA.Responsable, ["O-13"]));

        await UpdateAsync(id, new PartyFiscalAxesInput(
            RegimenTributario.Simple, ResponsabilidadIVA.NoResponsable, ["O-47"]));

        var f = await ReadFiscalAsync(id);
        f!.RegimenTributario.ShouldBe(RegimenTributario.Simple);
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.NoResponsable);
        f.ResponsabilidadesFiscales.ShouldBe(new[] { "O-47" });
    }

    [Fact]
    public async Task AllEmpty_FiscalAxes_Does_Not_Wipe_Populated_FiscalData()
    {
        // Guarda anti-wipe: un FiscalAxes vacío-total sobre un party con régimen NO lo borra.
        var num = $"fa-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, new PartyFiscalAxesInput(
            RegimenTributario.Ordinario, ResponsabilidadIVA.Responsable, ["O-13"]));

        await UpdateAsync(id, new PartyFiscalAxesInput(null, null, [])); // vacío-total

        var f = await ReadFiscalAsync(id);
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);    // PRESERVADO
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable); // PRESERVADO
        f.ResponsabilidadesFiscales.ShouldBe(new[] { "O-13" });        // PRESERVADO
    }

    [Fact]
    public async Task Clearing_One_Axis_Leaving_Other_Is_Authoritative()
    {
        // Limpiar UN eje dejando el otro NO dispara la guarda (no es vacío-total) → autoritativo.
        var num = $"fa-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, new PartyFiscalAxesInput(
            RegimenTributario.Ordinario, ResponsabilidadIVA.Responsable, []));

        await UpdateAsync(id, new PartyFiscalAxesInput(RegimenTributario.Ordinario, null, []));

        var f = await ReadFiscalAsync(id);
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        f.ResponsabilidadIVA.ShouldBeNull(); // limpiado deliberadamente
    }

    [Fact]
    public async Task Legacy_TaxRegimeCode_Path_Still_Works_Without_FiscalAxes()
    {
        var num = $"fa-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, axes: null, taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA");

        var f = await ReadFiscalAsync(id);
        f!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        f.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
    }
}
