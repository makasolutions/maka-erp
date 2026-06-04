using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDetailDto>;
