using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Brands.ListTrashedBrands;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;

public sealed class ListTrashedBrandsQueryHandler(CatalogDbContext db)
    : IQueryHandler<ListTrashedBrandsQuery, PagedResponse<TrashedBrandDto>>
{
    public async ValueTask<PagedResponse<TrashedBrandDto>> Handle(
        ListTrashedBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var brands = db.Brands
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            brands = brands.Where(b =>
                EF.Functions.ILike(b.Name, pattern) ||
                EF.Functions.ILike(b.Slug, pattern));
        }

        brands = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"      => brands.OrderBy(b => b.Name),
            "-name"     => brands.OrderByDescending(b => b.Name),
            "deletedat" => brands.OrderBy(b => b.DeletedOnUtc),
            _           => brands.OrderByDescending(b => b.DeletedOnUtc),
        };

        return await brands
            .Select(b => new TrashedBrandDto(
                b.Id,
                b.Name,
                b.Slug,
                b.IsActive,
                b.DeletedOnUtc,
                b.DeletedBy))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
