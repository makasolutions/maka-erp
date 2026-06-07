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
        var variations = await db.Variations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(v => variationIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Sku, v.ProductId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var skus = variations.ToDictionary(v => v.Id, v => v.Sku);
        var productByVariation = variations.ToDictionary(v => v.Id, v => v.ProductId);

        var productIds = variations.Select(v => v.ProductId).Distinct().ToList();
        var products = await db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                Thumbnail = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                            ?? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            })
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        return items
            .Select(b =>
            {
                Guid? itemProductId = productByVariation.TryGetValue(b.ItemVariationId, out var pid) ? pid : null;
                var prod = itemProductId is { } id && products.TryGetValue(id, out var pp) ? pp : null;
                return new BundleItemDto(
                    b.Id,
                    b.ProductId,
                    b.ItemVariationId,
                    skus.GetValueOrDefault(b.ItemVariationId, string.Empty),
                    b.Quantity,
                    b.DiscountPercent,
                    b.DiscountFixed,
                    b.IsOptional,
                    b.SortOrder,
                    itemProductId,
                    prod?.Name,
                    prod?.Thumbnail);
            })
            .ToList();
    }
}
