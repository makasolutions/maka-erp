using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributeById;

public sealed record AttributeDetailDto(
    Guid                          Id,
    string                        Name,
    string                        Slug,
    CatalogAttributeType          Type,
    bool                          IsVisibleOnProduct,
    bool                          IsUsedForVariations,
    int                           SortOrder,
    int?                          WooCommerceId,
    DateTime                      CreatedAtUtc,
    DateTime?                     UpdatedAtUtc,
    IReadOnlyList<AttributeValueDto> Values);
