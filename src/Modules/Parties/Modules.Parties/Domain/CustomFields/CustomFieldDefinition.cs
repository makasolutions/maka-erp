using System.Text;
using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.CustomFields;

/// <summary>
/// Definición (esquema) de un campo personalizado, modelo Attio (SPEC §3). Tenant-scoped (hereda el
/// filtro de <c>BaseDbContext</c>; NO es <c>IGlobalEntity</c>). Los VALORES viven en la columna JSONB
/// de la entidad scopeada (p. ej. <c>Party.CustomFields</c>), keyados por <see cref="ApiSlug"/>.
///
/// Invariantes:
/// - <see cref="EntityType"/>, <see cref="ApiSlug"/> y <see cref="FieldType"/> son INMUTABLES tras
///   crear (los referencian los valores ya guardados; cambiarlos los huérfanaría o rompería el tipo).
/// - <see cref="IsRequired"/> es de "completitud gobernada": NO bloquea la captura mínima del registro
///   dueño; se exige solo al ejecutar la acción de "completar" (ver <see cref="CustomFieldValues"/>).
/// - <see cref="IsUnique"/> se ALMACENA pero su enforcement a nivel BD está DIFERIDO [DISEÑO]: no hay
///   constraint simple sobre una clave dentro de un jsonb. No se hace un check app-level race-prone que
///   aparente garantía. TODO(futuro): índice de expresión/GIN sobre <c>(EntityType, slug-extraído)</c>.
/// - Select/MultiSelect exigen <see cref="Options"/> no vacío; los demás tipos las ignoran.
/// </summary>
public sealed class CustomFieldDefinition : BaseEntity<Guid>
{
    public CustomFieldEntityType EntityType { get; private set; }
    public string Title { get; private set; } = default!;
    public string ApiSlug { get; private set; } = default!;
    public CustomFieldType FieldType { get; private set; }
    public string? Description { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsUnique { get; private set; }
    public bool IsDefaultValueEnabled { get; private set; }

    /// <summary>Valor por defecto type-agnostic (string/JSON crudo). Se coerciona/valida por
    /// <see cref="FieldType"/> al usarse (ajuste C). Solo aplica si <see cref="IsDefaultValueEnabled"/>.</summary>
    public string? DefaultValue { get; private set; }

    public bool IsMultiselect { get; private set; }
    public IReadOnlyList<CustomFieldOption> Options { get; private set; } = [];
    public bool Activo { get; private set; } = true;

    private CustomFieldDefinition() { }

    public static CustomFieldDefinition Create(
        CustomFieldEntityType entityType,
        string title,
        string? apiSlug,
        CustomFieldType fieldType,
        string? description,
        bool isRequired,
        bool isUnique,
        bool isDefaultValueEnabled,
        string? defaultValue,
        bool isMultiselect,
        IReadOnlyList<CustomFieldOption>? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        string slug = NormalizeSlug(string.IsNullOrWhiteSpace(apiSlug) ? title : apiSlug);
        if (slug.Length == 0) throw new ArgumentException("El slug del campo no puede quedar vacío.", nameof(apiSlug));

        var opts = NormalizeOptions(fieldType, options);

        return new CustomFieldDefinition
        {
            Id = Guid.CreateVersion7(),
            EntityType = entityType,
            Title = title.Trim(),
            ApiSlug = slug,
            FieldType = fieldType,
            Description = description?.Trim(),
            IsRequired = isRequired,
            IsUnique = isUnique,
            IsDefaultValueEnabled = isDefaultValueEnabled,
            DefaultValue = isDefaultValueEnabled ? defaultValue?.Trim() : null,
            IsMultiselect = isMultiselect || fieldType == CustomFieldType.MultiSelect,
            Options = opts,
            Activo = true,
        };
    }

    /// <summary>Edita los campos mutables. NO toca <see cref="EntityType"/>/<see cref="ApiSlug"/>/
    /// <see cref="FieldType"/> (inmutables).</summary>
    public void Update(
        string title,
        string? description,
        bool isRequired,
        bool isUnique,
        bool isDefaultValueEnabled,
        string? defaultValue,
        bool isMultiselect,
        IReadOnlyList<CustomFieldOption>? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
        Description = description?.Trim();
        IsRequired = isRequired;
        IsUnique = isUnique;
        IsDefaultValueEnabled = isDefaultValueEnabled;
        DefaultValue = isDefaultValueEnabled ? defaultValue?.Trim() : null;
        IsMultiselect = isMultiselect || FieldType == CustomFieldType.MultiSelect;
        Options = NormalizeOptions(FieldType, options);
    }

    public void Deactivate() => Activo = false;
    public void Reactivate() => Activo = true;

    private static IReadOnlyList<CustomFieldOption> NormalizeOptions(
        CustomFieldType fieldType, IReadOnlyList<CustomFieldOption>? options)
    {
        bool isChoice = fieldType is CustomFieldType.Select or CustomFieldType.MultiSelect;
        if (!isChoice) return [];

        var clean = (options ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o.Value))
            .Select(o => new CustomFieldOption(o.Value.Trim(), string.IsNullOrWhiteSpace(o.Label) ? o.Value.Trim() : o.Label.Trim(), string.IsNullOrWhiteSpace(o.Color) ? null : o.Color.Trim()))
            .ToList();

        if (clean.Count == 0)
            throw new ArgumentException("Los campos Select/MultiSelect requieren al menos una opción.", nameof(options));

        var distinct = clean.Select(o => o.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct != clean.Count)
            throw new ArgumentException("Las opciones no pueden repetir el mismo valor.", nameof(options));

        return clean;
    }

    /// <summary>Normaliza un texto a slug estable: minúsculas, ascii, espacios/símbolos → <c>_</c>,
    /// sin <c>_</c> repetidos ni en los bordes. Máx 64 (alineado al límite de la columna).</summary>
    public static string NormalizeSlug(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var sb = new StringBuilder(raw.Length);
        bool lastUnderscore = false;
        foreach (char ch in raw.Trim().ToLowerInvariant())
        {
            char c = ch switch
            {
                'á' or 'à' or 'ä' or 'â' => 'a',
                'é' or 'è' or 'ë' or 'ê' => 'e',
                'í' or 'ì' or 'ï' or 'î' => 'i',
                'ó' or 'ò' or 'ö' or 'ô' => 'o',
                'ú' or 'ù' or 'ü' or 'û' => 'u',
                'ñ' => 'n',
                _ => ch,
            };
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                sb.Append(c);
                lastUnderscore = false;
            }
            else if (!lastUnderscore && sb.Length > 0)
            {
                sb.Append('_');
                lastUnderscore = true;
            }
        }
        string slug = sb.ToString().Trim('_');
        return slug.Length > 64 ? slug[..64].Trim('_') : slug;
    }
}
