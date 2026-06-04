using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.GetVariationsByProduct;

public sealed record GetVariationsByProductQuery(Guid ProductId) : IQuery<IReadOnlyList<VariationDto>>;
