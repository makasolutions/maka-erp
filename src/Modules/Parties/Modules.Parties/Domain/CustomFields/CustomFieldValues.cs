using System.Globalization;
using System.Text.Json;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.CustomFields;

/// <summary>Modo de validación de completitud gobernada (CLAUDE.md §17/§18, SPEC §2.1/§8.2).
/// <c>Minimal</c> = captura mínima: NO exige los <c>IsRequired</c>. <c>Complete</c> = acción de
/// "completar"/facturar: sí los exige.</summary>
public enum CustomFieldCompletenessMode
{
    Minimal = 0,
    Complete = 1,
}

/// <summary>
/// Validación/coerción de VALORES de custom fields contra sus definiciones. Sin estado; operación de
/// dominio pura (sin I/O). Los valores llegan como un diccionario <c>slug → JsonElement</c> (lo que se
/// deserializa del JSONB de la entidad dueña).
///
/// Reglas (ajustes aprobados):
/// - <b>A · IsRequired gobernado:</b> en <see cref="CustomFieldCompletenessMode.Minimal"/> un requerido
///   vacío NO es error; solo lo es en <see cref="CustomFieldCompletenessMode.Complete"/>.
/// - <b>C · DefaultValue/valor type-agnostic:</b> el valor se coerciona/valida por <c>FieldType</c>.
/// - <b>B · IsUnique:</b> NO se valida acá (sería un check app-level race-prone). El flag se respeta a
///   nivel de definición; el enforcement BD está diferido [DISEÑO].
/// </summary>
public static class CustomFieldValues
{
    /// <summary>Valida el valor de UNA definición. Devuelve el mensaje de error (español) o null si OK.
    /// Un valor ausente/nulo es válido aquí (la obligatoriedad la decide <see cref="ValidateAll"/>).</summary>
    public static string? ValidateValue(CustomFieldDefinition definition, JsonElement value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;

        switch (definition.FieldType)
        {
            case CustomFieldType.Text:
                return value.ValueKind == JsonValueKind.String ? null : Expected(definition, "texto");

            case CustomFieldType.EmailAddress:
                if (value.ValueKind != JsonValueKind.String) return Expected(definition, "texto");
                string email = value.GetString() ?? string.Empty;
                return email.Length == 0 || (email.Contains('@', StringComparison.Ordinal) && email.Contains('.', StringComparison.Ordinal))
                    ? null : $"'{definition.Title}': correo inválido.";

            case CustomFieldType.PhoneNumber:
                return value.ValueKind == JsonValueKind.String ? null : Expected(definition, "texto");

            case CustomFieldType.Number:
            case CustomFieldType.Currency:
                return value.ValueKind == JsonValueKind.Number ? null : Expected(definition, "número");

            case CustomFieldType.Checkbox:
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : Expected(definition, "booleano");

            case CustomFieldType.Date:
                if (value.ValueKind != JsonValueKind.String) return Expected(definition, "fecha (texto ISO)");
                return DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    ? null : $"'{definition.Title}': fecha inválida.";

            case CustomFieldType.Select:
                if (value.ValueKind != JsonValueKind.String) return Expected(definition, "una opción");
                return IsAllowed(definition, value.GetString()) ? null
                    : $"'{definition.Title}': el valor no está entre las opciones permitidas.";

            case CustomFieldType.MultiSelect:
                if (value.ValueKind != JsonValueKind.Array) return Expected(definition, "lista de opciones");
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || !IsAllowed(definition, item.GetString()))
                        return $"'{definition.Title}': hay valores fuera de las opciones permitidas.";
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Valida un conjunto de valores contra las definiciones ACTIVAS de un <c>EntityType</c>. Aplica la
    /// completitud gobernada (ajuste A): los <c>IsRequired</c> solo se exigen en modo
    /// <see cref="CustomFieldCompletenessMode.Complete"/>. Devuelve la lista de errores (vacía = OK).
    /// </summary>
    public static IReadOnlyList<string> ValidateAll(
        IEnumerable<CustomFieldDefinition> activeDefinitions,
        IReadOnlyDictionary<string, JsonElement> values,
        CustomFieldCompletenessMode mode)
    {
        ArgumentNullException.ThrowIfNull(activeDefinitions);
        ArgumentNullException.ThrowIfNull(values);

        var errors = new List<string>();
        foreach (var def in activeDefinitions)
        {
            bool present = values.TryGetValue(def.ApiSlug, out var value) && !IsEmpty(value);

            if (!present)
            {
                if (def.IsRequired && mode == CustomFieldCompletenessMode.Complete)
                    errors.Add($"'{def.Title}': es obligatorio para completar.");
                continue;
            }

            string? error = ValidateValue(def, value);
            if (error is not null) errors.Add(error);
        }
        return errors;
    }

    private static bool IsAllowed(CustomFieldDefinition def, string? candidate) =>
        candidate is not null && def.Options.Any(o => string.Equals(o.Value, candidate, StringComparison.Ordinal));

    private static bool IsEmpty(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
        JsonValueKind.Array => value.GetArrayLength() == 0,
        _ => false,
    };

    private static string Expected(CustomFieldDefinition def, string what) =>
        $"'{def.Title}': se esperaba {what}.";
}
