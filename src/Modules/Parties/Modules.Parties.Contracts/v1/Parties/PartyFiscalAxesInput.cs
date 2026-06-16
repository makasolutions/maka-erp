using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties;

/// <summary>
/// Ejes fiscales v2 (SPEC §4/§13) como input de escritura — alternativa AUTORITATIVA al
/// <c>TaxRegimeCode</c> string v1. Cuando un comando trae <c>FiscalAxes</c> (≠ null), el
/// synchronizer escribe estos ejes directamente en <c>FiscalData</c> (sin el reverse/forward-map
/// difuso del código plano); cuando es null, se usa el camino legacy (<c>TaxRegimeCode</c>).
/// El front lo envía SIEMPRE reflejando el estado del formulario (ver guarda anti-wipe en el handler).
/// </summary>
public sealed record PartyFiscalAxesInput(
    RegimenTributario?    RegimenTributario,
    ResponsabilidadIVA?   ResponsabilidadIVA,
    IReadOnlyList<string>? ResponsabilidadesFiscales);
