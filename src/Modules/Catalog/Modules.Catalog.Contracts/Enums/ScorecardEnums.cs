using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Estado de un scorecard de proveedor (Fase F). <c>Borrador</c> editable;
/// <c>Cerrado</c> inmutable (§8/§ACC-6) y disponible para ranking/convenios.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScorecardStatus>))]
public enum ScorecardStatus
{
    Borrador,
    Cerrado,
}

/// <summary>Letra de calificación derivada del score ponderado (1–5).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScorecardGrade>))]
public enum ScorecardGrade
{
    A,
    B,
    C,
    D,
    F,
}
