using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Bundles.RemoveBundleItem;

public sealed record RemoveBundleItemCommand(
    Guid ProductId,
    Guid ItemId) : ICommand<Guid>;
