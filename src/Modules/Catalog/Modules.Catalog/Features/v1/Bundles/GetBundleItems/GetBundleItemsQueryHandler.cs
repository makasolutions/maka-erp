using FSH.Modules.Catalog.Contracts.v1.Bundles.GetBundleItems;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Bundles.GetBundleItems;

public sealed class GetBundleItemsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetBundleItemsQuery, IReadOnlyList<BundleItemDto>>
{
    public async ValueTask<IReadOnlyList<BundleItemDto>> Handle(GetBundleItemsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var items = await db.Set<ProductBundleItem>()
            .AsNoTracking()
            .Where(b => b.ProductId == query.ProductId)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (items.Count == 0) return [];

        var variationIds = items.Select(b => b.ItemVariationId).Distinct().ToList();
        var skus = await db.Variations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(v => variationIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Sku, cancellationToken)
            .ConfigureAwait(false);

        return items
            .Select(b => new BundleItemDto(
                b.Id,
                b.ProductId,
                b.ItemVariationId,
                skus.GetValueOrDefault(b.ItemVariationId, string.Empty),
                b.Quantity,
                b.DiscountPercent,
                b.DiscountFixed,
                b.IsOptional,
                b.SortOrder))
            .ToList();
    }
}
