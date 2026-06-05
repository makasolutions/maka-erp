using FSH.Modules.Catalog.Contracts.v1.ProductTags.GetProductTags;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductTags.GetProductTags;

public sealed class GetProductTagsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductTagsQuery, IReadOnlyList<ProductTagDto>>
{
    public async ValueTask<IReadOnlyList<ProductTagDto>> Handle(GetProductTagsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.Set<ProductTag>()
            .AsNoTracking()
            .Where(t => t.ProductId == query.ProductId)
            .OrderBy(t => t.Name)
            .Select(t => new ProductTagDto(t.Id, t.ProductId, t.Name, t.Color))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
