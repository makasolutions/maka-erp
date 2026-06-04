using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Products.ListTrashedProducts;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.ListTrashedProducts;

public sealed class ListTrashedProductsQueryHandler(CatalogDbContext db)
    : IQueryHandler<ListTrashedProductsQuery, PagedResponse<TrashedProductDto>>
{
    public async ValueTask<PagedResponse<TrashedProductDto>> Handle(
        ListTrashedProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Slug, pattern));
        }

        products = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"      => products.OrderBy(p => p.Name),
            "-name"     => products.OrderByDescending(p => p.Name),
            "deletedat" => products.OrderBy(p => p.DeletedOnUtc),
            _           => products.OrderByDescending(p => p.DeletedOnUtc),
        };

        return await products
            .Select(p => new TrashedProductDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Type,
                p.Status,
                p.DeletedOnUtc,
                p.DeletedBy))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
