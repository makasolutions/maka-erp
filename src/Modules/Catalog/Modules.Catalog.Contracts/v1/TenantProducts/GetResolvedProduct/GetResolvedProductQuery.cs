using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TenantProducts.GetResolvedProduct;

public sealed record GetResolvedProductQuery(Guid CanonicalProductId) : IQuery<ResolvedProductDto>;
