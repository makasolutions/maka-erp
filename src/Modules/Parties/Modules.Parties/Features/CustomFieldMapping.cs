using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Domain.CustomFields;

namespace FSH.Modules.Parties.Features;

/// <summary>Mapeo manual (sin AutoMapper, regla del repo) entre la entidad de dominio y el DTO/Contracts.</summary>
internal static class CustomFieldMapping
{
    public static CustomFieldDefinitionDto ToDto(this CustomFieldDefinition d)
    {
        ArgumentNullException.ThrowIfNull(d);
        return new CustomFieldDefinitionDto(
            d.Id, d.EntityType, d.Title, d.ApiSlug, d.FieldType, d.Description,
            d.IsRequired, d.IsUnique, d.IsDefaultValueEnabled, d.DefaultValue, d.IsMultiselect,
            d.Options.Select(o => new CustomFieldOptionDto(o.Value, o.Label, o.Color)).ToList(),
            d.Activo);
    }

    public static IReadOnlyList<CustomFieldOption> ToDomain(this IReadOnlyList<CustomFieldOptionDto>? options) =>
        (options ?? []).Select(o => new CustomFieldOption(o.Value, o.Label, o.Color)).ToList();
}
