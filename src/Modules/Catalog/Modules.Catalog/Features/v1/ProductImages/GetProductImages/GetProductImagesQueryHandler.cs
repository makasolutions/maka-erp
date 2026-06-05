using FSH.Modules.Catalog.Contracts.v1.ProductImages.GetProductImages;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.GetProductImages;

public sealed class GetProductImagesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductImagesQuery, IReadOnlyList<ProductImageDto>>
{
    public async ValueTask<IReadOnlyList<ProductImageDto>> Handle(GetProductImagesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.Set<ProductImage>()
            .AsNoTracking()
            .Where(i => i.ProductId == query.ProductId)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.CreatedAtUtc)
            .Select(i => new ProductImageDto(
                i.Id,
                i.ProductId,
                i.Url,
                i.AltText,
                i.IsPrimary,
                i.SortOrder))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
