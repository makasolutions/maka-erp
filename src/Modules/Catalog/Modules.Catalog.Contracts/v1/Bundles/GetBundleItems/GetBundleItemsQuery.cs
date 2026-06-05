using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Bundles.GetBundleItems;

public sealed record GetBundleItemsQuery(Guid ProductId) : IQuery<IReadOnlyList<BundleItemDto>>;
