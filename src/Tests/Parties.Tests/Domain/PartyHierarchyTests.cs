using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Exceptions;

namespace Parties.Tests.Domain;

/// <summary>
/// PR-D3: jerarquía de terceros (ParentPartyId) + delegación comercial (ResolveCommercialEntity).
/// La validación de ciclos es pura en el dominio: recibe el conjunto de ids de ancestros que el
/// caller carga con un recursive CTE.
/// </summary>
public class PartyHierarchyTests
{
    private static Party NewParty(string num) =>
        Party.Create("NIT", num, 1, PartyKind.Juridica, $"Tercero {num}");

    [Fact]
    public void AssignParent_Happy_SetsParent()
    {
        var sucursal = NewParty("100");
        var matriz = NewParty("200");

        sucursal.AssignParent(matriz.Id, new HashSet<Guid>());

        sucursal.ParentPartyId.ShouldBe(matriz.Id);
    }

    [Fact]
    public void AssignParent_DirectCycle_Throws()
    {
        var p = NewParty("100");
        Should.Throw<PartyHierarchyCycleException>(() => p.AssignParent(p.Id, new HashSet<Guid>()));
    }

    [Fact]
    public void AssignParent_TransitiveCycle_Throws()
    {
        // A→B→C→A: asignar a A el padre C, cuyos ancestros son {B, A}. A ya es ancestro de C → ciclo.
        var a = NewParty("100");
        var b = NewParty("200");
        var c = NewParty("300");
        var cAncestors = new HashSet<Guid> { b.Id, a.Id };

        Should.Throw<PartyHierarchyCycleException>(() => a.AssignParent(c.Id, cAncestors));
    }

    [Fact]
    public void ResolveCommercialEntity_ChildEmpty_ResolvesFromMatriz()
    {
        var sucursal = NewParty("100"); // sin FiscalData propia
        var matriz = NewParty("200");
        matriz.AssignFiscalData(new FiscalData { RegimenTributario = RegimenTributario.Ordinario });

        var commercial = sucursal.ResolveCommercialEntity([matriz]);

        commercial.Id.ShouldBe(matriz.Id);
        commercial.FiscalData!.RegimenTributario.ShouldBe(RegimenTributario.Ordinario);
    }

    [Fact]
    public void ResolveCommercialEntity_ChildHasOwnData_ReturnsSelf()
    {
        var sucursal = NewParty("100");
        sucursal.AssignFiscalData(new FiscalData { RegimenTributario = RegimenTributario.Simple });
        var matriz = NewParty("200");
        matriz.AssignFiscalData(new FiscalData { RegimenTributario = RegimenTributario.Ordinario });

        var commercial = sucursal.ResolveCommercialEntity([matriz]);

        commercial.Id.ShouldBe(sucursal.Id); // no sobrescribe sus datos propios
        commercial.FiscalData!.RegimenTributario.ShouldBe(RegimenTributario.Simple);
    }
}
