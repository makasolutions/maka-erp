using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TaxRates.CreateTaxRate;

public sealed record CreateTaxRateCommand(
    string  Name,
    decimal Rate,
    string? Description = null,
    bool    IsDefault   = false) : ICommand<Guid>;
