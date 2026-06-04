using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceLists;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceLists;

public sealed class GetPriceListsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPriceListsQuery, PagedResponse<PriceListDto>>
{
    public async ValueTask<PagedResponse<PriceListDto>> Handle(GetPriceListsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var lists = db.PriceLists.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            lists = lists.Where(p => EF.Functions.ILike(p.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerSegment))
        {
            string segment = query.CustomerSegment.Trim().ToLowerInvariant();
            lists = lists.Where(p => p.CustomerSegment == segment);
        }

        if (query.IsActive.HasValue)
            lists = lists.Where(p => p.IsActive == query.IsActive.Value);

        lists = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"       => lists.OrderBy(p => p.Name),
            "-name"      => lists.OrderByDescending(p => p.Name),
            "validfrom"  => lists.OrderBy(p => p.ValidFrom),
            _            => lists.OrderByDescending(p => p.ValidFrom),
        };

        return await lists
            .Select(p => new PriceListDto(
                p.Id,
                p.Name,
                p.Description,
                p.CustomerSegment,
                p.ValidFrom,
                p.ValidTo,
                p.IsActive,
                p.Items.Count,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
