namespace FSH.Modules.Catalog.Contracts.v1.Categories.GetCategoryById;

public sealed record CategoryDetailDto(
    Guid      Id,
    Guid?     ParentId,
    string    Name,
    string    Slug,
    string?   Description,
    string?   ImageUrl,
    int       SortOrder,
    bool      IsActive,
    int?      WooCommerceId,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc);
