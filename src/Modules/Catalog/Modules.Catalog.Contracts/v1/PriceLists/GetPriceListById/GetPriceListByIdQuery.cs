using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceListById;

public sealed record GetPriceListByIdQuery(Guid Id) : IQuery<PriceListDetailDto>;
