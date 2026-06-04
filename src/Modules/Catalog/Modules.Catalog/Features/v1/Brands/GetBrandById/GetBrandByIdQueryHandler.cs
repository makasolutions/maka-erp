using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands.GetBrandById;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;

public sealed class GetBrandByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetBrandByIdQuery, BrandDetailDto>
{
    public async ValueTask<BrandDetailDto> Handle(GetBrandByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var brand = await db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.Id == query.Id)
            .Select(b => new BrandDetailDto(
                b.Id,
                b.Name,
                b.Slug,
                b.Description,
                b.LogoUrl,
                b.WebsiteUrl,
                b.CountryOfOrigin,
                b.IsActive,
                b.WooCommerceId,
                b.CreatedAtUtc,
                b.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return brand ?? throw new NotFoundException($"Brand {query.Id} not found.");
    }
}
