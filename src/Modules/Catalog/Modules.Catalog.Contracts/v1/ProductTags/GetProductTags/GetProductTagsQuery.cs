using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductTags.GetProductTags;

public sealed record GetProductTagsQuery(Guid ProductId) : IQuery<IReadOnlyList<ProductTagDto>>;
