using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Tipo de atributo de catálogo — spec §2.5.
/// Prefijo "Catalog" para evitar conflicto con System.Attribute.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogAttributeType>))]
public enum CatalogAttributeType { Text, Color, Image, Select }
