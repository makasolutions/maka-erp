using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Marketplaces externos con requisitos de atributos por categoría (Fase 2 §RF).
/// Google Shopping y Mercado Libre exigen ciertos atributos para publicar.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Marketplace>))]
public enum Marketplace { Google, MercadoLibre }
