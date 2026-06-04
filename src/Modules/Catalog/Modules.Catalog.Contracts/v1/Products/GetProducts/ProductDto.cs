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
    int?           WooCommerceId,
    DateTime       CreatedAtUtc,
    DateTime?      UpdatedAtUtc);
