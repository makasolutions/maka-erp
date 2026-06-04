using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;

public sealed record CreatePriceListCommand(
    string    Name,
    string    CustomerSegment,
    DateTime? ValidFrom   = null,
    string?   Description = null) : ICommand<Guid>;
