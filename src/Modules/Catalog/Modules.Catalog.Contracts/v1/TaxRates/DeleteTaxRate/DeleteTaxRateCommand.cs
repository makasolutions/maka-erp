using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TaxRates.DeleteTaxRate;

public sealed record DeleteTaxRateCommand(Guid Id) : ICommand<Guid>;
