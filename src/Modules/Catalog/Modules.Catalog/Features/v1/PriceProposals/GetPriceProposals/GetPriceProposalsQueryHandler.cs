using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.GetPriceProposals;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.GetPriceProposals;

public sealed class GetPriceProposalsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPriceProposalsQuery, PagedResponse<PriceProposalDto>>
{
    public async ValueTask<PagedResponse<PriceProposalDto>> Handle(GetPriceProposalsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var proposals = db.PriceBulkProposals.AsNoTracking();

        if (query.BatchId.HasValue)
            proposals = proposals.Where(p => p.BatchId == query.BatchId.Value);

        if (query.Status.HasValue)
            proposals = proposals.Where(p => p.Status == query.Status.Value);

        proposals = proposals.OrderByDescending(p => p.CreatedAtUtc);

        return await proposals
            .Select(p => new PriceProposalDto(
                p.Id,
                p.BatchId,
                p.PriceListId,
                p.VariationId,
                db.Variations.IgnoreQueryFilters().Where(v => v.Id == p.VariationId).Select(v => (string?)v.Sku).FirstOrDefault(),
                p.SupplierId,
                p.SupplierCode,
                p.OldPrice,
                p.NewPrice,
                p.Status,
                p.ChangeReason,
                p.SourceReference,
                p.CreatedAtUtc,
                p.CreatedByUserId,
                p.DecidedAtUtc,
                p.DecidedByUserId))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
