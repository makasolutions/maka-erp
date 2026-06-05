using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductTags.SetProductTags;

public sealed record ProductTagInput(
    string  Name,
    string? Color = null);

/// <summary>
/// Replaces the full set of tags for a product (spec §2.10).
/// An empty list removes all tags.
/// </summary>
public sealed record SetProductTagsCommand(
    Guid                            ProductId,
    IReadOnlyList<ProductTagInput>  Tags) : ICommand<Guid>;
