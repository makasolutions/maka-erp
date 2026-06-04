namespace FSH.Modules.Catalog.Contracts.v1.Variations.GetVariationsByProduct;

public sealed record VariationDto(
    Guid      Id,
    Guid      ProductId,
    string    Sku,
    string?   Description,
    bool      IsDefault,
    bool      IsActive,
    bool      IsDeleted,
    decimal?  Weight,
    string?   WeightUnit,
    string?   ImageUrl,
    bool      ManageStock,
    bool      AllowBackorders,
    bool      SoldIndividually,
    int?      LowStockThreshold,
    bool      IsVirtual,
    int?      WooCommerceId,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc);
