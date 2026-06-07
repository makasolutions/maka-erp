using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Tipo de lista de precios (Fase 3/4).
/// Segment = lista de precios por segmento (una por defecto, las demás derivan por %).
/// Campaign = oferta/campaña temporal con vigencia que prevalece sobre la lista.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PriceListKind>))]
public enum PriceListKind { Segment, Campaign }
