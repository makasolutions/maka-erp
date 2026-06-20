using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

/// <summary>
/// Entidad a la que se aplica una definición de campo personalizado (el "scope", SPEC §3.4).
/// <c>Party</c> → atributo global del tercero (viaja con el registro). <c>PartyRelationship</c> →
/// atributo de contexto de la relación (se crea en PR-2; la definición ya lo admite).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CustomFieldEntityType>))]
public enum CustomFieldEntityType : short
{
    Party = 1,
    PartyRelationship = 2,
}

/// <summary>
/// Tipo de un campo personalizado (subconjunto de Attio, SPEC §3.2 + decisión de Juan 2026-06-19:
/// Text·Number·Currency·Date·Checkbox·Select·MultiSelect·EmailAddress·PhoneNumber). Los avanzados
/// (Rating/RecordReference/Timestamp/Actor/Interaction/Location/Status) se difieren.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CustomFieldType>))]
public enum CustomFieldType : short
{
    Text = 1,
    Number = 2,
    Currency = 3,
    Date = 4,
    Checkbox = 5,
    Select = 6,
    MultiSelect = 7,
    EmailAddress = 8,
    PhoneNumber = 9,
}
