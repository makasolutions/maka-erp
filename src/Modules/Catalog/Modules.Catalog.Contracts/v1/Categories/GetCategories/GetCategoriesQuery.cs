using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.GetCategories;

/// <summary>
/// Returns the full category tree (roots with nested children).
/// Not paginated — the tree is typically small (&lt;200 nodes) and loaded once.
/// </summary>
public sealed record GetCategoriesQuery(
    bool? IsActive = null,
    Guid? ParentId = null) : IQuery<IReadOnlyList<CategoryDto>>;
