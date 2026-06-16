using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

// Divergencia v1↔v2: v1 usa códigos de Tabla Básica (Lookups) para el tipo de
// identificación (`IdentificationTypeCode` string); v2 usa este enum fuerte.
// TODO(PR-D): resolver el mapeo enum↔código cuando se enlace Party v2 — al
// reshapear Party, decidir si la identificación del tercero migra a enum o si
// el enum convive con los códigos de Tabla Básica (p.ej. tabla de equivalencias).
/// <summary>Tipo de documento de identificación (modelo v2, SPEC §13).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TipoIdentificacion>))]
public enum TipoIdentificacion : short
{
    CC = 1,
    CE = 2,
    NIT = 3,
    TI = 4,
    Pasaporte = 5,
    NUIP = 6,
    NITExtranjero = 7,
    RUT = 8,
    PEP = 9, // Permiso Especial de Permanencia
}
