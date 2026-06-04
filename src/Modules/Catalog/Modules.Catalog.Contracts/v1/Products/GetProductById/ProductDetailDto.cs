using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetProductById;

public sealed record ProductDetailDto(
    Guid           Id,
    string         Name,
    string         Slug,
    string?        ShortDescription,
    string?        Description,
    string?        TechnicalSpecs,
    string?        ThumbnailUrl,
    Guid?          BrandId,
    string?        BrandName,
    Guid?          TaxRateId,
    Guid?          ShippingClassId,
    ProductType    Type,
    ProductStatus  Status,
    bool           IsVirtual,
    bool           IsDownloadable,
    bool           IsPublic,
    decimal?       Weight,
    string         WeightUnit,
    decimal?       DimensionLength,
    decimal?       DimensionWidth,
    decimal?       DimensionHeight,
    string         DimensionUnit,
    string?        SeoTitle,
    string?        SeoDescription,
    string?        SeoKeywords,
    int?           WooCommerceId,
    DateTime       CreatedAtUtc,
    DateTime?      UpdatedAtUtc,
    IReadOnlyList<ProductImageDto>             Images,
    IReadOnlyList<ProductVariationSummaryDto>  Variations,
    IReadOnlyList<ProductCategoryDto>          Categories,
    IReadOnlyList<string>                      Tags);

public sealed record ProductImageDto(
    Guid    Id,
    string  Url,
    string? AltText,
    bool    IsPrimary,
    int     SortOrder);

public sealed record ProductVariationSummaryDto(
    Guid    Id,
    string  Sku,
    string? Description,
    bool    IsDefault,
    bool    IsActive);

public sealed record ProductCategoryDto(
    Guid   Id,
    string Name,
    string Slug,
    bool   IsPrimary);
