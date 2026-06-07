using System.Text.Json.Serialization;

namespace FSH.Modules.Catalog.Contracts.Enums;

/// <summary>
/// Tipo de lista de precios (Fase 3/4).
/// Segment = lista de precios por segmento (una por defecto, las demás derivan por %).
/// Campaign = oferta/campaña temporal con vigencia que prevalece sobre la lista.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PriceListKind>))]
public enum PriceListKind { Segment, Campaign }

/// <summary>
/// Estado de una campaña (Fase 4). Scheduled = jobs programados; Running = vigente
/// (precede a las listas de segmento); Ended = terminada (revierte); Cancelled.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CampaignStatus>))]
public enum CampaignStatus { Scheduled, Running, Ended, Cancelled }
