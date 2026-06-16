using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

// Modelo v2 (SPEC §13) — ejes fiscales SEPARADOS (regla de modelado R4):
// el régimen de renta y la responsabilidad de IVA son ejes ortogonales,
// no un solo campo como en v1 (`TaxRegimeCode` string).

/// <summary>Régimen de renta (norma DIAN 2026). Un solo eje — RST es un valor, no un bool aparte.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RegimenTributario>))]
public enum RegimenTributario : short
{
    Ordinario = 1,
    Simple = 2,
    Especial = 3,
}

/// <summary>Responsabilidad de IVA. Eje independiente del régimen de renta.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ResponsabilidadIVA>))]
public enum ResponsabilidadIVA : short
{
    Responsable = 1,
    NoResponsable = 2,
}
