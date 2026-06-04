using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Prices.GetEffectivePrice;

public sealed record GetEffectivePriceQuery(
    Guid    VariationId,
    string? Segment = null) : IQuery<EffectivePriceDto>;
