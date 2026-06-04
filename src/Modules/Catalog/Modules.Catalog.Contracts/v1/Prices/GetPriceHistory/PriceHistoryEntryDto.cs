namespace FSH.Modules.Catalog.Contracts.v1.Prices.GetPriceHistory;

public sealed record PriceHistoryEntryDto(
    Guid      Id,
    Guid      PriceListItemId,
    Guid      VariationId,
    decimal   OldPrice,
    decimal   NewPrice,
    decimal?  OldSalePrice,
    decimal?  NewSalePrice,
    DateTime  ChangedAt,
    string    ChangedByUserId,
    string?   ChangeReason,
    string?   SourceReference);
