using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.GenerateVariations;

/// <summary>
/// Generates one variation per attribute-value combination, from the product's
/// attributes marked <c>IsUsedForVariations</c> (cartesian product, spec §5.4).
/// Existing combinations are skipped.
/// </summary>
public sealed record GenerateVariationsCommand(Guid ProductId) : ICommand<GenerateVariationsResult>;

public sealed record GenerateVariationsResult(
    int                 Created,
    int                 Skipped,
    IReadOnlyList<Guid> CreatedVariationIds);
