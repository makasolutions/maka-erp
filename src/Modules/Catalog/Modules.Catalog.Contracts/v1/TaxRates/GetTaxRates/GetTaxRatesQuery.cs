using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TaxRates.GetTaxRates;

public sealed record GetTaxRatesQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<TaxRateDto>>;
