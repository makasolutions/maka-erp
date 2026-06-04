using FSH.Modules.Catalog.Contracts.v1.Prices.GetPriceHistory;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetPriceHistory;

public sealed class GetPriceHistoryQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPriceHistoryQuery, IReadOnlyList<PriceHistoryEntryDto>>
{
    public async ValueTask<IReadOnlyList<PriceHistoryEntryDto>> Handle(GetPriceHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.Set<PriceListItemHistory>()
            .AsNoTracking()
            .Where(h => h.VariationId == query.VariationId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new PriceHistoryEntryDto(
                h.Id,
                h.PriceListItemId,
                h.VariationId,
                h.OldPrice,
                h.NewPrice,
                h.OldSalePrice,
                h.NewSalePrice,
                h.ChangedAt,
                h.ChangedByUserId,
                h.ChangeReason,
                h.SourceReference))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
