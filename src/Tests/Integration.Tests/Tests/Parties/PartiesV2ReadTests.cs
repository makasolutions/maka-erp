using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Parties.GetParties;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-D5d — lado de lectura: GetPartyById (detalle rico) y GetParties (resumen barato) exponen v2
/// de forma ADITIVA bajo <c>V2</c>. Verifica además que la proyección EF traduce en runtime el
/// owned-of-owned (SupplierProfile.PaymentTerms.DiasCredito) y la colección con value-converter
/// (FiscalData.ResponsabilidadesFiscales) — cosas que el compilador no garantiza.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2ReadTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2ReadTests(FshWebApplicationFactory factory) => _factory = factory;

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

    private Task<PartyDetailDto> GetDetailAsync(Guid id) =>
        InScope(sp => sp.GetRequiredService<IQueryHandler<GetPartyByIdQuery, PartyDetailDto>>()
            .Handle(new GetPartyByIdQuery(id), default).AsTask());

    private Task<PagedResponse<PartyDto>> GetListAsync(string search) =>
        InScope(sp => sp.GetRequiredService<IQueryHandler<GetPartiesQuery, PagedResponse<PartyDto>>>()
            .Handle(new GetPartiesQuery { PageSize = 200, Search = search }, default).AsTask());

    [Fact]
    public async Task Detail_Exposes_Full_V2_Fiscal_Ciiu_Credit_And_Profiles()
    {
        var num = $"r5-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, PartyRole.Customer | PartyRole.Supplier,
            creditLimit: 1_000_000m, creditBlocked: true,
            taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA", ciiu: "4791");

        var dto = await GetDetailAsync(id);

        dto.V2.ShouldNotBeNull();
        // Ejes fiscales derivados del dual-write
        dto.V2!.Fiscal.ShouldNotBeNull();
        dto.V2.Fiscal!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        dto.V2.Fiscal.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
        // CIIU principal
        dto.V2.CiiuActivities.Count.ShouldBe(1);
        dto.V2.CiiuActivities[0].CiiuCode.ShouldBe("4791");
        dto.V2.CiiuActivities[0].IsPrincipal.ShouldBeTrue();
        // Profiles (owned-of-owned: PaymentTerms.DiasCredito traduce en runtime)
        dto.V2.Customer.ShouldNotBeNull();
        dto.V2.Customer!.IsActive.ShouldBeTrue();
        dto.V2.Supplier.ShouldNotBeNull();
        dto.V2.Supplier!.IsActive.ShouldBeTrue();
        // Crédito vía follow-up query
        dto.V2.Credit.ShouldNotBeNull();
        dto.V2.Credit!.CupoAsignado.ShouldBe(1_000_000m);
        dto.V2.Credit.EstaActivo.ShouldBeTrue();
        // En M1 SaldoDisponible == CupoAsignado (provisional, sin consumos)
        dto.V2.Credit.SaldoDisponible.ShouldBe(1_000_000m);
        // Hold activo (CreditBlocked → Ventas) vía follow-up query
        dto.V2.ActiveHolds.ShouldContain(h => h.HoldType == HoldType.Ventas);
    }

    [Fact]
    public async Task Detail_Keeps_V1_Fields_Intact_Alongside_V2()
    {
        var num = $"r5-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, PartyRole.Customer, taxRegimeCode: "SIMPLE");

        var dto = await GetDetailAsync(id);

        // Frontera v1: los campos legacy siguen ahí, sin alteración por la exposición v2.
        dto.Id.ShouldBe(id);
        dto.IdentificationNumber.ShouldBe(num);
        dto.Kind.ShouldBe(PartyKind.Juridica);
        dto.TaxRegimeCode.ShouldBe("SIMPLE");
        dto.Roles.ShouldBe(PartyRole.Customer);
        // y v2 coexiste
        dto.V2!.Fiscal!.RegimenTributario.ShouldBe(RegimenTributario.Simple);
    }

    [Fact]
    public async Task Detail_Without_V2_Data_Returns_Empty_V2_Not_Null()
    {
        // Sin crédito, sin CIIU, sin régimen mapeable → V2 presente pero con campos nulos/vacíos.
        var num = $"r5-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, PartyRole.Customer);

        var dto = await GetDetailAsync(id);

        dto.V2.ShouldNotBeNull();
        dto.V2!.Credit.ShouldBeNull();
        dto.V2.ActiveHolds.ShouldBeEmpty();
        dto.V2.CiiuActivities.ShouldBeEmpty();
        dto.V2.Supplier.ShouldBeNull();        // no tiene rol Supplier
    }

    [Fact]
    public async Task List_Exposes_Minimal_V2_Summary()
    {
        var num = $"r5-{Guid.NewGuid():N}"[..13];
        var id = await CreateAsync(num, PartyRole.Customer, taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA");

        var page = await GetListAsync(num);

        var row = page.Items.Where(r => r.Id == id).ShouldHaveSingleItem();
        row.V2.ShouldNotBeNull();
        row.V2!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        row.V2.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
        row.V2.ParentPartyId.ShouldBeNull();
        // v1 intacto en la fila
        row.IdentificationNumber.ShouldBe(num);
        row.Kind.ShouldBe(PartyKind.Juridica);
    }
}
