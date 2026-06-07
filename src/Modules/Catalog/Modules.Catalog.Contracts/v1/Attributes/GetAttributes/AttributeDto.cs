using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributes;

public sealed record AttributeDto(
    Guid                 Id,
    string               Name,
    string               Slug,
    CatalogAttributeType Type,
    bool                 IsVisibleOnProduct,
    bool                 IsUsedForVariations,
    int                  SortOrder,
    int                  ValueCount,
    int?                 WooCommerceId,
    IReadOnlyList<Guid>  CategoryIds);
