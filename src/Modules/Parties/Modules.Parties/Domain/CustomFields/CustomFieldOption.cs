namespace FSH.Modules.Parties.Domain.CustomFields;

/// <summary>
/// Opción de un campo Select/MultiSelect. <c>Value</c> es la clave estable que se persiste en el
/// JSONB de valores (no se renombra); <c>Label</c> es el texto visible; <c>Color</c> es opcional
/// (token CSS o hex) para el chip. Se serializa como parte del JSONB <c>Options</c> de la definición.
/// </summary>
public sealed record CustomFieldOption(string Value, string Label, string? Color);
