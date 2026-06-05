using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.GetProducts;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProducts;

public sealed class GetProductsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductsQuery, PagedResponse<ProductDto>>
{
    public async ValueTask<PagedResponse<ProductDto>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Slug, pattern));
        }

        if (query.BrandId.HasValue)
            products = products.Where(p => p.BrandId == query.BrandId.Value);

        if (query.CategoryId.HasValue)
            products = products.Where(p =>
                db.Set<ProductCategory>().Any(pc => pc.ProductId == p.Id && pc.CategoryId == query.CategoryId.Value));

        if (query.Type.HasValue)
            products = products.Where(p => p.Type == query.Type.Value);

        if (query.Status.HasValue)
            products = products.Where(p => p.Status == query.Status.Value);

        products = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"      => products.OrderBy(p => p.Name),
            "-name"     => products.OrderByDescending(p => p.Name),
            "createdat" => products.OrderBy(p => p.CreatedAtUtc),
            "-createdat"=> products.OrderByDescending(p => p.CreatedAtUtc),
            "status"    => products.OrderBy(p => p.Status),
            _           => products.OrderBy(p => p.Name),
        };

        return await products
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Slug,
                p.ShortDescription,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                p.BrandId,
                p.BrandId == null ? null : db.Brands.Where(b => b.Id == p.BrandId).Select(b => (string?)b.Name).FirstOrDefault(),
                p.Type,
                p.Status,
                p.IsVirtual,
                p.IsPublic,
                p.Variations.Where(v => v.IsDefault && !v.IsDeleted).Select(v => (string?)v.Sku).FirstOrDefault(),
                db.Set<ProductCategory>()
                    .Where(pc => pc.ProductId == p.Id && pc.IsPrimary)
                    .Select(pc => (Guid?)pc.CategoryId)
                    .FirstOrDefault(),
                db.Set<ProductCategory>()
                    .Where(pc => pc.ProductId == p.Id && pc.IsPrimary)
                    .Join(db.Categories, pc => pc.CategoryId, c => c.Id, (pc, c) => (string?)c.Name)
                    .FirstOrDefault(),
                p.WooCommerceId,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
