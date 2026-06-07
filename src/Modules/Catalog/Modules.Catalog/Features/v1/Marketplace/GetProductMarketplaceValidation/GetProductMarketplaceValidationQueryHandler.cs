using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetProductMarketplaceValidation;

/// <summary>
/// Non-blocking coverage check: for the product's categories, which
/// marketplace-required attributes are missing (not assigned to the product).
/// </summary>
public sealed class GetProductMarketplaceValidationQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductMarketplaceValidationQuery, ProductMarketplaceValidationDto>
{
    public async ValueTask<ProductMarketplaceValidationDto> Handle(
        GetProductMarketplaceValidationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categoryIds = await db.Set<ProductCategory>().AsNoTracking()
            .Where(pc => pc.ProductId == query.ProductId)
            .Select(pc => pc.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var coveredAttributeIds = await db.Set<ProductAttribute>().AsNoTracking()
            .Where(pa => pa.ProductId == query.ProductId)
            .Select(pa => pa.AttributeId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var covered = coveredAttributeIds.ToHashSet();

        var requirements = await (
            from r in db.MarketplaceAttributeRequirements.AsNoTracking()
            where categoryIds.Contains(r.CategoryId)
            join a in db.Attributes.AsNoTracking() on r.AttributeId equals a.Id
            select new { r.Marketplace, a.Id, a.Name })
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = requirements
            .GroupBy(x => x.Marketplace)
            .Select(g =>
            {
                var required = g
                    .DistinctBy(x => x.Id)
                    .Select(x => new AttributeRef(x.Id, x.Name))
                    .ToList();
                var missing = required.Where(a => !covered.Contains(a.Id)).ToList();
                return new MarketplaceValidationGroup(g.Key, required, missing);
            })
            .ToList();

        return new ProductMarketplaceValidationDto(query.ProductId, groups);
    }
}
