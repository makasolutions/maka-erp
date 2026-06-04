using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Prices.GetPriceHistory;

public sealed record GetPriceHistoryQuery(Guid VariationId)
    : IQuery<IReadOnlyList<PriceHistoryEntryDto>>;
