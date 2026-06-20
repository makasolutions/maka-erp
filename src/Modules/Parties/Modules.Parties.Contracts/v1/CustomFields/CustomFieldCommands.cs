using FSH.Modules.Parties.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.CustomFields;

/// <summary>
/// Crea una definición de campo personalizado (cambio de ESQUEMA → permiso admin
/// <c>Parties.CustomFields.Define</c>). <c>EntityType</c>, <c>ApiSlug</c> y <c>FieldType</c> son
/// inmutables tras crear (los referencian los valores guardados en JSONB). Si <c>ApiSlug</c> es null
/// se deriva del <c>Title</c>.
/// </summary>
public sealed record CreateCustomFieldDefinitionCommand(
    CustomFieldEntityType EntityType,
    string Title,
    string? ApiSlug,
    CustomFieldType FieldType,
    string? Description,
    bool IsRequired,
    bool IsUnique,
    bool IsDefaultValueEnabled,
    string? DefaultValue,
    bool IsMultiselect,
    IReadOnlyList<CustomFieldOptionDto>? Options) : ICommand<Guid>;

/// <summary>Edita una definición. NO cambia <c>EntityType</c>/<c>ApiSlug</c>/<c>FieldType</c>
/// (inmutables). Permiso admin <c>Parties.CustomFields.Define</c>.</summary>
public sealed record UpdateCustomFieldDefinitionCommand(
    Guid Id,
    string Title,
    string? Description,
    bool IsRequired,
    bool IsUnique,
    bool IsDefaultValueEnabled,
    string? DefaultValue,
    bool IsMultiselect,
    IReadOnlyList<CustomFieldOptionDto>? Options) : ICommand;

/// <summary>Baja lógica de una definición (<c>Activo=false</c>). No borra los valores ya guardados.
/// Permiso admin <c>Parties.CustomFields.Define</c>.</summary>
public sealed record DeactivateCustomFieldDefinitionCommand(Guid Id) : ICommand;

/// <summary>Lista las definiciones del tenant para un <c>EntityType</c> (o todas). Por defecto solo
/// las activas. Permiso <c>Parties.CustomFields.View</c>.</summary>
public sealed record GetCustomFieldDefinitionsQuery(
    CustomFieldEntityType? EntityType = null,
    bool IncludeInactive = false) : IQuery<IReadOnlyList<CustomFieldDefinitionDto>>;
