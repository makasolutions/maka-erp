using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;

public sealed record CreatePriceListCommand(
    string    Name,
    string    CustomerSegment,
    DateTime? ValidFrom         = null,
    DateTime? ValidTo           = null,
    string?   Description       = null,
    bool      IsDefault         = false,
    decimal?  AdjustmentPercent = null,
    bool      RoundEnabled      = true) : ICommand<Guid>;
