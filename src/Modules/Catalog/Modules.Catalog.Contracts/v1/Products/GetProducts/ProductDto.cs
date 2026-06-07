using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetProducts;

public sealed record ProductDto(
    Guid           Id,
    string         Name,
    string         Slug,
    string?        ShortDescription,
    string?        ThumbnailUrl,
    Guid?          BrandId,
    string?        BrandName,
    ProductType    Type,
    ProductStatus  Status,
    bool           IsVirtual,
    bool           IsPublic,
    string?        DefaultSku,
    Guid?          PrimaryCategoryId,
    string?        PrimaryCategoryName,
    int?           WooCommerceId,
    DateTime       CreatedAtUtc,
    DateTime?      UpdatedAtUtc,
    decimal?       DefaultPrice,
    decimal?       MinVariationPrice,
    decimal?       MaxVariationPrice,
    IReadOnlyList<ProductCodeBriefDto> Codes);

/// <summary>A product code shown in the catalog grid (SKU + EAN/UPC/…).</summary>
public sealed record ProductCodeBriefDto(string CodeType, string Code);
