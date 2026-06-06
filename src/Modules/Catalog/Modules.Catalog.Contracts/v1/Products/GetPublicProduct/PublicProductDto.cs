using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetPublicProduct;

/// <summary>Public, anonymous-safe projection of a product for a shareable sheet (Active only).</summary>
public sealed record PublicProductDto(
    Guid           Id,
    string         Name,
    string         Slug,
    string?        BrandName,
    ProductType    Type,
    string?        ShortDescription,
    string?        Description,
    string?        TechnicalSpecs,
    string?        Specs,
    IReadOnlyList<PublicImageDto>     Images,
    IReadOnlyList<PublicCodeDto>      Codes,
    IReadOnlyList<PublicVariationDto> Variations);

public sealed record PublicImageDto(string Url, string? AltText, bool IsPrimary, int SortOrder);
public sealed record PublicCodeDto(string CodeType, string Code);
public sealed record PublicVariationDto(string Sku, string Combination);
