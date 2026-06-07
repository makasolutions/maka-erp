namespace FSH.Modules.Catalog.Contracts.v1.Bundles.GetBundleItems;

public sealed record BundleItemDto(
    Guid     Id,
    Guid     ProductId,
    Guid     ItemVariationId,
    string   ItemSku,
    int      Quantity,
    decimal? DiscountPercent,
    decimal? DiscountFixed,
    bool     IsOptional,
    int      SortOrder,
    Guid?    ItemProductId   = null,
    string?  ItemProductName = null,
    string?  ItemThumbnailUrl = null);
