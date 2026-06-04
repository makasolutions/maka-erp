using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.GetBrandById;

public sealed record GetBrandByIdQuery(Guid Id) : IQuery<BrandDetailDto>;
