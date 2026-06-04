using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceListById;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceListById;

public sealed class GetPriceListByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPriceListByIdQuery, PriceListDetailDto>
{
    public async ValueTask<PriceListDetailDto> Handle(GetPriceListByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var priceList = await db.PriceLists
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.Id == query.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Price list {query.Id} not found.");

        var variationIds = priceList.Items.Select(i => i.VariationId).ToList();
        var skus = variationIds.Count == 0
            ? []
            : await db.Variations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(v => variationIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Sku })
                .ToDictionaryAsync(v => v.Id, v => v.Sku, cancellationToken)
                .ConfigureAwait(false);

        var items = priceList.Items
            .OrderBy(i => i.CreatedAt)
            .Select(i => new PriceListItemDto(
                i.Id,
                i.VariationId,
                skus.TryGetValue(i.VariationId, out var sku) ? sku : null,
                i.Price,
                i.MinQuantity,
                i.SalePrice,
                i.SalePriceFrom,
                i.SalePriceTo,
                i.CreatedAt,
                i.UpdatedAtUtc))
            .ToList();

        return new PriceListDetailDto(
            priceList.Id,
            priceList.Name,
            priceList.Description,
            priceList.CustomerSegment,
            priceList.ValidFrom,
            priceList.ValidTo,
            priceList.IsActive,
            priceList.CreatedAtUtc,
            priceList.UpdatedAtUtc,
            items);
    }
}
