using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.SetProductAttributes;

/// <summary>
/// One attribute assigned to a product, with the subset of its values selected
/// for this product and whether it drives variation generation (spec §2.12).
/// </summary>
public sealed record ProductAttributeAssignment(
    Guid                  AttributeId,
    IReadOnlyList<Guid>   ValueIds,
    bool                  IsUsedForVariations = false,
    bool                  IsVisibleOnProduct  = true,
    int                   SortOrder           = 0);

/// <summary>
/// Replaces the full set of attribute assignments for a product.
/// An empty list removes all attribute assignments.
/// </summary>
public sealed record SetProductAttributesCommand(
    Guid                                       ProductId,
    IReadOnlyList<ProductAttributeAssignment>  Attributes) : ICommand<Guid>;
