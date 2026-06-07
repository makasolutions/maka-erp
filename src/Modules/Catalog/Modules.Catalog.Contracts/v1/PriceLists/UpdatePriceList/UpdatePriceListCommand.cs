using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceList;

public sealed record UpdatePriceListCommand(
    Guid      Id,
    string    Name,
    string?   Description,
    DateTime  ValidFrom,
    DateTime? ValidTo,
    bool      IsActive,
    bool      IsDefault,
    decimal?  AdjustmentPercent,
    bool      RoundEnabled = true) : ICommand<Guid>;
