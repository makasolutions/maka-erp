using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Migration;

/// <summary>
/// Mapea el <c>TaxRegimeCode</c> string de v1 a los dos ejes fiscales v2 separados
/// (<see cref="RegimenTributario"/> + <see cref="ResponsabilidadIVA"/>, SPEC §4/§13).
///
/// PR-C NO persiste el resultado: <c>FiscalData</c> es owned VO de <c>Party</c>, inlined en la
/// tabla <c>Parties</c>, que no se reshapea hasta PR-D. Aquí el mapper solo ANALIZA y reporta la
/// deuda de reconciliación (cuántos códigos v1 mapean limpio vs cuántos no). PR-D usará este mismo
/// mapper para persistir. Doctrina: si no mapea limpio, NO se adivina — se reporta para revisión.
/// </summary>
public static class TaxRegimeMapper
{
    /// <summary>
    /// Intenta derivar (régimen, responsabilidadIVA) del código v1. Devuelve <c>mapped=false</c>
    /// cuando el código no es derivable con confianza (queda para revisión humana en PR-D).
    /// </summary>
    public static TaxRegimeMapping Map(string? taxRegimeCode)
    {
        if (string.IsNullOrWhiteSpace(taxRegimeCode))
        {
            // Sin código v1 → nada que reconciliar (no es un "no mapeado", simplemente no había dato).
            return new TaxRegimeMapping(Mapped: true, Regimen: null, ResponsabilidadIVA: null, IsEmpty: true);
        }

        var code = taxRegimeCode.Trim().ToUpperInvariant();

        RegimenTributario? regimen = code switch
        {
            _ when code.Contains("SIMPLE", StringComparison.Ordinal) || code.Contains("RST", StringComparison.Ordinal) => RegimenTributario.Simple,
            _ when code.Contains("ESPECIAL", StringComparison.Ordinal) => RegimenTributario.Especial,
            _ when code.Contains("ORDINARIO", StringComparison.Ordinal) || code.Contains("COMUN", StringComparison.Ordinal) || code.Contains("COMÚN", StringComparison.Ordinal) => RegimenTributario.Ordinario,
            _ => null,
        };

        ResponsabilidadIVA? iva = code switch
        {
            _ when code.Contains("NO_RESPONSABLE", StringComparison.Ordinal) || code.Contains("NORESPONSABLE", StringComparison.Ordinal) || code.Contains("SIMPLIFICADO", StringComparison.Ordinal) => ResponsabilidadIVA.NoResponsable,
            _ when code.Contains("RESPONSABLE", StringComparison.Ordinal) || code.Contains("COMUN", StringComparison.Ordinal) || code.Contains("COMÚN", StringComparison.Ordinal) => ResponsabilidadIVA.Responsable,
            _ => null,
        };

        // "Mapea limpio" = al menos un eje derivable. Si NINGÚN eje se derivó, es deuda de revisión.
        bool mapped = regimen is not null || iva is not null;
        return new TaxRegimeMapping(mapped, regimen, iva, IsEmpty: false);
    }
}

/// <summary>Resultado del análisis de un <c>TaxRegimeCode</c> v1.</summary>
/// <param name="Mapped">True si al menos un eje se derivó (o si no había código). False = deuda PR-D.</param>
/// <param name="Regimen">Régimen derivado, o null.</param>
/// <param name="ResponsabilidadIVA">Responsabilidad IVA derivada, o null.</param>
/// <param name="IsEmpty">True si el código v1 estaba vacío (no es deuda, simplemente no había dato).</param>
public sealed record TaxRegimeMapping(
    bool Mapped,
    RegimenTributario? Regimen,
    ResponsabilidadIVA? ResponsabilidadIVA,
    bool IsEmpty);
