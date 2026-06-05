using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Bundles.AddBundleItem;

public sealed record AddBundleItemCommand(
    Guid     ProductId,
    Guid     ItemVariationId,
    int      Quantity        = 1,
    decimal? DiscountPercent = null,
    decimal? DiscountFixed   = null,
    bool     IsOptional      = false,
    int      SortOrder       = 0) : ICommand<Guid>;
