using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Identidad fiscal del tercero (modelo v2, SPEC §4). Value object inmutable.
///
/// Ejes SEPARADOS (regla de modelado R4): <see cref="RegimenTributario"/> (renta) y
/// <see cref="ResponsabilidadIVA"/> son ortogonales — uno puede fijarse sin el otro.
///
/// PR-A: VO independiente, NO está embebido en el <c>Party</c> v1 todavía. Se incrustará
/// como owned VO del Party en PR-D (cuando se reshapee el agregado). Sin mapeo EF aún.
/// </summary>
public sealed record FiscalData
{
    public RegimenTributario? RegimenTributario { get; init; }
    public ResponsabilidadIVA? ResponsabilidadIVA { get; init; }
    public IReadOnlyList<string> ResponsabilidadesFiscales { get; init; } = [];
    public string? FormaJuridica { get; init; }
    public bool GranContribuyente { get; init; }
    public bool Autorretenedor { get; init; }
    public bool AgenteRetencionIVA { get; init; }
    public bool AgenteRetencionICA { get; init; }
    public bool ObligadoLlevarContabilidad { get; init; }
    public bool FlagPEP { get; init; }

    /// <summary>VO vacío (tercero sin datos fiscales capturados todavía).</summary>
    public static FiscalData Empty { get; } = new();
}
