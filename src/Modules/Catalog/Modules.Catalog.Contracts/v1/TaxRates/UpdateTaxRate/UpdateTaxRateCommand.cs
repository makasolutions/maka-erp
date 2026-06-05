using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TaxRates.UpdateTaxRate;

public sealed record UpdateTaxRateCommand(
    Guid    Id,
    string  Name,
    decimal Rate,
    string? Description,
    bool    IsDefault,
    bool    IsActive) : ICommand<Guid>;
