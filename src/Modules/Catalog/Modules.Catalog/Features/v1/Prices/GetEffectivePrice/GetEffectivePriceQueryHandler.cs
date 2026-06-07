using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Prices.GetEffectivePrice;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetEffectivePrice;

public sealed class GetEffectivePriceQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetEffectivePriceQuery, EffectivePriceDto>
{
    public async ValueTask<EffectivePriceDto> Handle(GetEffectivePriceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string segment = string.IsNullOrWhiteSpace(query.Segment)
            ? "retail"
            : query.Segment.Trim().ToLowerInvariant();

        var now = DateTime.UtcNow;

        // Precedence: a Running campaign that includes this variation wins over segment lists.
        var campaign = await (
            from item in db.PriceListItems.AsNoTracking()
            join list in db.PriceLists.AsNoTracking() on item.PriceListId equals list.Id
            where item.VariationId == query.VariationId
               && list.ListKind == PriceListKind.Campaign
               && list.CampaignStatus == CampaignStatus.Running
            orderby list.ValidFrom descending
            select new { item.Price, list.Name, list.Id })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (campaign is not null)
            return new EffectivePriceDto(
                query.VariationId, campaign.Price, campaign.Price, false,
                "campaign", campaign.Name, campaign.Id, IsCampaign: true);

        var match = await ResolveAsync(query.VariationId, segment, now, cancellationToken).ConfigureAwait(false);

        // Fallback to retail if the requested segment has no active list/price (spec §13).
        if (match is null && segment != "retail")
        {
            segment = "retail";
            match = await ResolveAsync(query.VariationId, segment, now, cancellationToken).ConfigureAwait(false);
        }

        if (match is null)
            throw new NotFoundException($"No active price found for variation {query.VariationId}.");

        bool saleActive = match.Item.SalePrice.HasValue
            && (match.Item.SalePriceFrom is null || match.Item.SalePriceFrom <= now)
            && (match.Item.SalePriceTo is null || match.Item.SalePriceTo >= now);

        decimal effective = saleActive ? match.Item.SalePrice!.Value : match.Item.Price;

        return new EffectivePriceDto(
            query.VariationId,
            effective,
            match.Item.Price,
            saleActive,
            match.List.CustomerSegment,
            match.List.Name,
            match.List.Id);
    }

    private async Task<PriceMatch?> ResolveAsync(Guid variationId, string segment, DateTime now, CancellationToken ct)
    {
        return await (
            from item in db.PriceListItems.AsNoTracking()
            join list in db.PriceLists.AsNoTracking() on item.PriceListId equals list.Id
            where item.VariationId == variationId
               && list.CustomerSegment == segment
               && list.ValidFrom <= now
               && (list.ValidTo == null || list.ValidTo > now)
            orderby list.ValidFrom descending
            select new PriceMatch(item, list))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private sealed record PriceMatch(PriceListItem Item, PriceList List);
}
