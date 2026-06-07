namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceListById;

public sealed record PriceListDetailDto(
    Guid      Id,
    string    Name,
    string?   Description,
    string    CustomerSegment,
    DateTime  ValidFrom,
    DateTime? ValidTo,
    bool      IsActive,
    bool      IsDefault,
    decimal?  AdjustmentPercent,
    bool      RoundEnabled,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<PriceListItemDto> Items);

public sealed record PriceListItemDto(
    Guid      Id,
    Guid      VariationId,
    string?   VariationSku,
    decimal   Price,
    decimal?  MinQuantity,
    bool      IsManualOverride,
    decimal?  SalePrice,
    DateTime? SalePriceFrom,
    DateTime? SalePriceTo,
    DateTime  CreatedAt,
    DateTime? UpdatedAtUtc);
