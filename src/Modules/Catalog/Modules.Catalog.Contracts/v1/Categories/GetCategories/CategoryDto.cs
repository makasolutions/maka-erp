namespace FSH.Modules.Catalog.Contracts.v1.Categories.GetCategories;

public sealed record CategoryDto(
    Guid    Id,
    Guid?   ParentId,
    string  Name,
    string  Slug,
    string? Description,
    string? ImageUrl,
    int     SortOrder,
    bool    IsActive,
    int?    WooCommerceId,
    IReadOnlyList<CategoryDto> Children);
