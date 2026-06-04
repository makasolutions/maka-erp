using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Brands.GetBrands;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrands;

public sealed class GetBrandsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetBrandsQuery, PagedResponse<BrandDto>>
{
    public async ValueTask<PagedResponse<BrandDto>> Handle(GetBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var brands = db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        if (query.IsActive.HasValue)
        {
            brands = brands.Where(b => b.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            brands = brands.Where(b =>
                EF.Functions.ILike(b.Name, pattern) ||
                EF.Functions.ILike(b.Slug, pattern));
        }

        brands = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"     => brands.OrderBy(b => b.Name),
            "-name"    => brands.OrderByDescending(b => b.Name),
            "createdat" => brands.OrderBy(b => b.CreatedAtUtc),
            _ => brands.OrderBy(b => b.Name),
        };

        return await brands
            .Select(b => new BrandDto(
                b.Id,
                b.Name,
                b.Slug,
                b.Description,
                b.LogoUrl,
                b.WebsiteUrl,
                b.CountryOfOrigin,
                b.IsActive,
                b.WooCommerceId))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
