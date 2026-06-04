namespace FSH.Modules.Catalog.Contracts.v1.Brands.GetBrandById;

public sealed record BrandDetailDto(
    Guid    Id,
    string  Name,
    string  Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? CountryOfOrigin,
    bool    IsActive,
    int?    WooCommerceId,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc);
