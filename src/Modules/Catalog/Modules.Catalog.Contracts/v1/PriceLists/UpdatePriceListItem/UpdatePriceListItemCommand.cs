using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceListItem;

public sealed record UpdatePriceListItemCommand(
    Guid      PriceListId,
    Guid      ItemId,
    decimal   Price,
    string?   ChangeReason  = null,
    decimal?  SalePrice     = null,
    DateTime? SalePriceFrom = null,
    DateTime? SalePriceTo   = null) : ICommand<Guid>;
