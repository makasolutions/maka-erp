using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.V2;

namespace Parties.Tests.Domain;

/// <summary>SPEC §4 / R4: régimen de renta y responsabilidad de IVA son ejes independientes.</summary>
public class FiscalDataTests
{
    [Fact]
    public void RegimenAndIva_AreIndependentAxes_SettableSeparately()
    {
        // Solo régimen, sin IVA.
        var soloRegimen = new FiscalData { RegimenTributario = RegimenTributario.Simple };
        soloRegimen.RegimenTributario.ShouldBe(RegimenTributario.Simple);
        soloRegimen.ResponsabilidadIVA.ShouldBeNull();

        // Solo IVA, sin régimen.
        var soloIva = new FiscalData { ResponsabilidadIVA = ResponsabilidadIVA.NoResponsable };
        soloIva.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.NoResponsable);
        soloIva.RegimenTributario.ShouldBeNull();

        // Ambos, combinación arbitraria (Especial + Responsable).
        var ambos = new FiscalData
        {
            RegimenTributario = RegimenTributario.Especial,
            ResponsabilidadIVA = ResponsabilidadIVA.Responsable,
        };
        ambos.RegimenTributario.ShouldBe(RegimenTributario.Especial);
        ambos.ResponsabilidadIVA.ShouldBe(ResponsabilidadIVA.Responsable);
    }

    [Fact]
    public void Empty_HasNoAxesSet()
    {
        FiscalData.Empty.RegimenTributario.ShouldBeNull();
        FiscalData.Empty.ResponsabilidadIVA.ShouldBeNull();
        FiscalData.Empty.ResponsabilidadesFiscales.ShouldBeEmpty();
    }
}
