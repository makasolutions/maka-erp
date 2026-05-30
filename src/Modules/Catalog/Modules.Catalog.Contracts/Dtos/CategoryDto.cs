namespace FSH.Modules.Catalog.Contracts.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Code,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentCategoryId,
    bool IsActive,
    bool IsVisible,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
