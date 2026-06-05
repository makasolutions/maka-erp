using FSH.Modules.Catalog.Contracts.v1.TaxRates.GetTaxRates;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.GetTaxRates;

public sealed class GetTaxRatesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetTaxRatesQuery, IReadOnlyList<TaxRateDto>>
{
    public async ValueTask<IReadOnlyList<TaxRateDto>> Handle(GetTaxRatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.TaxRates.AsNoTracking();
        if (query.ActiveOnly)
            q = q.Where(t => t.IsActive);

        return await q
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Rate)
            .Select(t => new TaxRateDto(t.Id, t.Name, t.Rate, t.Description, t.IsDefault, t.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
