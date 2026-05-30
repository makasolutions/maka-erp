using FSH.Modules.Catalog.Contracts.Dtos;
using FSH.Modules.Catalog.Contracts.v1.Products;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductStats;

public sealed class GetProductStatsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetProductStatsQuery, ProductStatsDto>
{
    public async ValueTask<ProductStatsDto> Handle(GetProductStatsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Single round-trip: EF translates the conditional counts into one
        // SELECT with COUNT(CASE WHEN …) columns. The tenant query filter on the
        // DbContext keeps this scoped to the current tenant automatically.
        var stats = await dbContext.Products
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new ProductStatsDto(
                g.LongCount(),
                g.LongCount(p => p.IsActive),
                g.LongCount(p => p.IsVisible)))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return stats ?? new ProductStatsDto(0, 0, 0);
    }
}
