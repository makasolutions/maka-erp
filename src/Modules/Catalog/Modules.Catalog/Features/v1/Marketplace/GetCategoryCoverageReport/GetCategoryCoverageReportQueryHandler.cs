using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryCoverageReport;

/// <summary>
/// Audit report: for the category's expected attributes (its template, or a
/// marketplace's required set when a marketplace is given), which products in
/// the category have each attribute assigned.
/// </summary>
public sealed class GetCategoryCoverageReportQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetCategoryCoverageReportQuery, CategoryCoverageReportDto>
{
    public async ValueTask<CategoryCoverageReportDto> Handle(
        GetCategoryCoverageReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Expected attribute ids: marketplace requirements (if filtered) else the
        // category's attribute template.
        List<Guid> expectedIds = query.Marketplace is { } mk
            ? await db.MarketplaceAttributeRequirements.AsNoTracking()
                .Where(r => r.CategoryId == query.CategoryId && r.Marketplace == mk)
                .Select(r => r.AttributeId)
                .ToListAsync(cancellationToken).ConfigureAwait(false)
            : await db.CategoryAttributes.AsNoTracking()
                .Where(ca => ca.CategoryId == query.CategoryId)
                .OrderBy(ca => ca.SortOrder)
                .Select(ca => ca.AttributeId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        expectedIds = expectedIds.Distinct().ToList();

        var attributes = await db.Attributes.AsNoTracking()
            .Where(a => expectedIds.Contains(a.Id))
            .OrderBy(a => a.Name)
            .Select(a => new AttributeRef(a.Id, a.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Products in the category (non-deleted via the Products query filter).
        var products = await (
            from pc in db.Set<ProductCategory>().AsNoTracking()
            where pc.CategoryId == query.CategoryId
            join p in db.Products.AsNoTracking() on pc.ProductId equals p.Id
            select new { p.Id, p.Name })
            .Distinct()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var productIds = products.Select(p => p.Id).ToList();

        // Covered (productId, attributeId) pairs among the expected attributes.
        var coveredPairs = await db.Set<ProductAttribute>().AsNoTracking()
            .Where(pa => productIds.Contains(pa.ProductId) && expectedIds.Contains(pa.AttributeId))
            .Select(pa => new { pa.ProductId, pa.AttributeId })
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var coveredByProduct = coveredPairs
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.AttributeId).ToHashSet());

        var rows = products
            .Select(p => new CoverageProductRow(
                p.Id, p.Name,
                coveredByProduct.TryGetValue(p.Id, out var set) ? set.ToList() : []))
            .ToList();

        var summary = attributes
            .Select(a => new CoverageAttributeSummary(
                a.Id, a.Name,
                coveredPairs.Count(x => x.AttributeId == a.Id),
                products.Count))
            .ToList();

        return new CategoryCoverageReportDto(query.CategoryId, query.Marketplace, attributes, rows, summary);
    }
}
