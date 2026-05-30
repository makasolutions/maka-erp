using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Code,
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null,
    string? ImageUrl = null,
    bool IsActive = true,
    bool IsVisible = true) : ICommand<Guid>;
