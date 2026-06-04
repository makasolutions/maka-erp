using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string  Name,
    string? Slug,
    string? Description,
    string? ImageUrl,
    Guid?   ParentId,
    int     SortOrder = 0,
    bool    IsActive  = true) : ICommand<Guid>;
