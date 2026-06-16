using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Agreements;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using Integration.Tests.Infrastructure;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// PR-E — test guardián de frontera. Convenios (Catalog) consume el Party del distribuidor vía
/// <c>GetPartyByIdQuery</c>; tras la migración v2, ese handler proyecta <c>V2</c> pero los campos
/// v1 que Convenios lee (Kind/CreatedAtUtc/Identificación) son **base** y sobreviven intactos.
/// Este test crea el Party por el camino real (Create → dual-write puebla v2) y prueba que la
/// evaluación de reglas sigue idéntica con el <c>PartyDetailDto</c> que produce el modelo v2 —
/// validando el dual-write retroactivamente desde un consumidor real, y blindando PR-F (la
/// remoción de columnas v1 no puede cambiar este comportamiento sin romper este test o el build).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AgreementEvaluationV2Tests
{
    private readonly FshWebApplicationFactory _factory;

    public AgreementEvaluationV2Tests(FshWebApplicationFactory factory) => _factory = factory;

    private static AppTenantInfo Root => new(TestConstants.RootTenantId, TestConstants.RootTenantId);

    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(Root);
        return await action(scope.ServiceProvider);
    }

    [Fact]
    public async Task Evaluate_Pins_Convenios_Rules_Against_V2_Produced_PartyDetailDto()
    {
        // Party Juridica con identificación NIT, rol Supplier → el Create dual-escribe el modelo v2.
        var num = $"e5-{Guid.NewGuid():N}"[..13];
        var partyId = await InScope(sp => sp.GetRequiredService<ICommandHandler<CreatePartyCommand, Guid>>()
            .Handle(new CreatePartyCommand("NIT", num, null, PartyKind.Juridica, $"Distribuidor {num}",
                PartyRole.Supplier), default).AsTask());

        // Convenio con las 4 reglas que dependen de campos base del Party. El CreateAgreement handler
        // no valida existencia del SupplierId, pero usamos el propio Party para mantenerlo realista.
        var rules = new List<AgreementRuleInput>
        {
            new(AgreementRuleType.VendeAEmpresa,        null, null, null,  IsMandatory: true),  // Kind=Juridica → Cumple
            new(AgreementRuleType.VendeANatural,        null, null, null,  IsMandatory: false), // rama negativa → NoCumple
            new(AgreementRuleType.AntiguedadMinimaMeses, 0m,  null, null,  IsMandatory: true),  // recién creado → 0≥0 Cumple
            new(AgreementRuleType.DocumentoExigido,     null, null, "NIT", IsMandatory: true),  // IdTypeCode=NIT → Cumple
        };
        var agreementId = await InScope(sp => sp.GetRequiredService<ICommandHandler<CreateAgreementCommand, Guid>>()
            .Handle(new CreateAgreementCommand($"Convenio {num}", partyId, AgreementType.DistribuidorAprobado,
                null, null,
                AgreementResponsible.Proveedor, AgreementResponsible.Proveedor, AgreementResponsible.Proveedor,
                null, null, null, DateTime.UtcNow.AddDays(-1), null, null, rules), default).AsTask());

        var eval = await InScope(sp => sp.GetRequiredService<IQueryHandler<EvaluateAgreementQuery, AgreementEvaluationDto>>()
            .Handle(new EvaluateAgreementQuery(agreementId, partyId), default).AsTask());

        eval.DistributorPartyId.ShouldBe(partyId);

        RuleEvaluationResult Result(AgreementRuleType t) =>
            eval.Rules.Single(r => r.RuleType == t).Result;

        Result(AgreementRuleType.VendeAEmpresa).ShouldBe(RuleEvaluationResult.Cumple);        // Kind fluye (positivo)
        Result(AgreementRuleType.VendeANatural).ShouldBe(RuleEvaluationResult.NoCumple);      // Kind fluye (negativo)
        Result(AgreementRuleType.AntiguedadMinimaMeses).ShouldBe(RuleEvaluationResult.Cumple); // CreatedAtUtc base
        Result(AgreementRuleType.DocumentoExigido).ShouldBe(RuleEvaluationResult.Cumple);     // Identificación base

        // Elegible = ninguna mandatory en NoCumple (VendeANatural es no-mandatory).
        eval.Eligible.ShouldBeTrue();
    }
}
