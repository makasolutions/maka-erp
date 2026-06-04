using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductCodes.GetProductCodes;

public sealed record GetProductCodesQuery(Guid ProductId, Guid VariationId)
    : IQuery<IReadOnlyList<ProductCodeDto>>;
