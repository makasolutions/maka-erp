using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.SetProductCategories;

public sealed record ProductCategoryAssignment(
    Guid CategoryId,
    bool IsPrimary = false);

/// <summary>
/// Replaces the full set of category associations for a product.
/// An empty list removes all categories. Exactly one assignment may be primary
/// (if none is marked, the first becomes primary).
/// </summary>
public sealed record SetProductCategoriesCommand(
    Guid ProductId,
    IReadOnlyList<ProductCategoryAssignment> Categories) : ICommand<Guid>;
