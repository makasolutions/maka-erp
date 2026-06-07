using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryRequirements;

public sealed class GetCategoryRequirementsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetCategoryRequirementsQuery, CategoryRequirementsDto>
{
    public async ValueTask<CategoryRequirementsDto> Handle(GetCategoryRequirementsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = await db.MarketplaceAttributeRequirements
            .AsNoTracking()
            .Where(r => r.CategoryId == query.CategoryId)
            .Select(r => new { r.Marketplace, r.AttributeId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = rows
            .GroupBy(r => r.Marketplace)
            .Select(g => new MarketplaceRequirementGroup(g.Key, g.Select(x => x.AttributeId).ToList()))
            .ToList();

        return new CategoryRequirementsDto(query.CategoryId, groups);
    }
}
