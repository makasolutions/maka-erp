namespace FSH.Modules.Catalog.Contracts.v1.Prices.GetEffectivePrice;

public sealed record EffectivePriceDto(
    Guid    VariationId,
    decimal EffectivePrice,
    decimal ListPrice,
    bool    IsSalePrice,
    string  CustomerSegment,
    string  PriceListName,
    Guid    PriceListId,
    bool    IsCampaign = false);
