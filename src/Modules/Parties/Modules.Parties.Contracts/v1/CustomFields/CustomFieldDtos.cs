using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.CustomFields;

/// <summary>Opción de un campo Select/MultiSelect: <c>Value</c> estable (lo guardado), <c>Label</c>
/// visible, <c>Color</c> opcional (token o hex para el chip).</summary>
public sealed record CustomFieldOptionDto(string Value, string Label, string? Color);

/// <summary>Definición de un campo personalizado (esquema, no valor). Tenant-scoped. SPEC §3.</summary>
public sealed record CustomFieldDefinitionDto(
    Guid Id,
    CustomFieldEntityType EntityType,
    string Title,
    string ApiSlug,
    CustomFieldType FieldType,
    string? Description,
    bool IsRequired,
    bool IsUnique,
    bool IsDefaultValueEnabled,
    string? DefaultValue,
    bool IsMultiselect,
    IReadOnlyList<CustomFieldOptionDto> Options,
    bool Activo);
