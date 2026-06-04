namespace FSH.Modules.Catalog.Contracts.v1.Brands.GetBrands;

public sealed record BrandDto(
    Guid    Id,
    string  Name,
    string  Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? CountryOfOrigin,
    bool    IsActive,
    int?    WooCommerceId);
