using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.AddPriceListItem;

public sealed record AddPriceListItemCommand(
    Guid      PriceListId,
    Guid      VariationId,
    decimal   Price,
    decimal?  MinQuantity   = null,
    decimal?  SalePrice     = null,
    DateTime? SalePriceFrom = null,
    DateTime? SalePriceTo   = null) : ICommand<Guid>;
