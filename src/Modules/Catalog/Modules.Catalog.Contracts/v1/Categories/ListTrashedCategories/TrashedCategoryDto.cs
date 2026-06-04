namespace FSH.Modules.Catalog.Contracts.v1.Categories.ListTrashedCategories;

public sealed record TrashedCategoryDto(
    Guid            Id,
    Guid?           ParentId,
    string          Name,
    string          Slug,
    bool            IsActive,
    DateTimeOffset? DeletedOnUtc,
    string?         DeletedBy);
