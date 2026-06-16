using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Data;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Parties;

/// <summary>
/// PR-F1a — el output v1 del DTO se computa DESDE v2 (no de las columnas v1, que siguen presentes
/// pero ya no son la fuente de verdad de lectura). Y <c>SetPartyRoles</c> ahora sí sincroniza
/// profiles (gap pre-D5 cerrado). Las columnas v1 NO se dropean en F1a (reversible).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PartiesV2ComputedOutputTests
{
    private readonly FshWebApplicationFactory _factory;

    public PartiesV2ComputedOutputTests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Root => new(TestConstants.RootTenantId, TestConstants.RootTenantId);

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Root);
        return await action(scope.ServiceProvider);
    }

    private Task<Guid> CreateAsync(string num, PartyRole roles, decimal? creditLimit = null, string? taxRegimeCode = null) =>
        InScope(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Tercero {num}", roles,
                TaxRegimeCode: taxRegimeCode, CreditLimit: creditLimit), default).AsTask());

    private Task<PartyDetailDto> GetDetailAsync(Guid id) =>
        InScope(sp => sp.GetRequiredService<IQueryHandler<GetPartyByIdQuery, PartyDetailDto>>()
            .Handle(new GetPartyByIdQuery(id), default).AsTask());

    [Fact]
    public async Task Roles_And_Credit_Output_Reconstructed_From_V2_Without_Loss()
    {
        var num = $"f1a-{Guid.NewGuid():N}"[..14];
        var id = await CreateAsync(num, PartyRole.Customer | PartyRole.Supplier, creditLimit: 750_000m);

        var dto = await GetDetailAsync(id);

        dto.Roles.ShouldBe(PartyRole.Customer | PartyRole.Supplier); // desde facetas activas
        dto.HasCredit.ShouldBeTrue();
        dto.CreditLimit.ShouldBe(750_000m);                          // desde CreditAccount
    }

    [Fact]
    public async Task TaxRegimeCode_Output_Is_BestEffort_Canonical_Reverse_Map()
    {
        // PÉRDIDA CONOCIDA: el string v1 original no round-trips; vuelve un canónico equivalente.
        var num = $"f1a-{Guid.NewGuid():N}"[..14];
        var id = await CreateAsync(num, PartyRole.Customer, taxRegimeCode: "REGIMEN_COMUN_RESPONSABLE_IVA");

        var dto = await GetDetailAsync(id);

        // Los ejes v2 son la verdad; el campo v1 derivado es el canónico.
        dto.V2!.Fiscal!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
        dto.V2.Fiscal.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
        dto.TaxRegimeCode.ShouldBe("ORDINARIO_RESPONSABLE_IVA"); // canónico, NO el string original
    }

    [Fact]
    public async Task SetPartyRoles_Now_Syncs_Profiles_And_Reflects_In_Computed_Output()
    {
        var num = $"f1a-{Guid.NewGuid():N}"[..14];
        var id = await CreateAsync(num, PartyRole.Customer);

        // Antes de F1a, SetPartyRoles solo escribía el flag v1 (no creaba SupplierProfile) → el output
        // computado desde v2 NO mostraría Supplier. Ahora sincroniza la faceta.
        await InScope(sp => sp.GetRequiredService<ICommandHandler<SetPartyRolesCommand, Guid>>()
            .Handle(new SetPartyRolesCommand(id, PartyRole.Customer | PartyRole.Supplier), default).AsTask());

        var dto = await GetDetailAsync(id);
        dto.Roles.ShouldBe(PartyRole.Customer | PartyRole.Supplier);
        dto.V2!.Supplier.ShouldNotBeNull();
        dto.V2.Supplier!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task V1_Columns_Still_Present_After_F1a_Reversible()
    {
        // F1a es reversible: las columnas v1 siguen físicamente escritas por Party.Create.
        var num = $"f1a-{Guid.NewGuid():N}"[..14];
        var id = await CreateAsync(num, PartyRole.Customer | PartyRole.Supplier, creditLimit: 300_000m);

        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<PartiesDbContext>();
            var party = await db.Parties.SingleAsync(p => p.Id == id);
            party.Roles.ShouldBe(PartyRole.Customer | PartyRole.Supplier); // columna v1 aún escrita
            party.CreditLimit.ShouldBe(300_000m);                          // columna v1 aún escrita
            return 0;
        });
    }
}
