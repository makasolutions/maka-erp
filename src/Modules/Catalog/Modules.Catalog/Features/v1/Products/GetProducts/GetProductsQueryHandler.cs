using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
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

        // Filter by any code (default-variation SKU or any product code).
        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            string pat = $"%{query.Code.Trim()}%";
            products = products.Where(p =>
                p.Variations.Any(v => v.IsDefault && !v.IsDeleted && EF.Functions.ILike(v.Sku, pat)) ||
                db.ProductCodes.Any(c => c.ProductId == p.Id && EF.Functions.ILike(c.Code, pat)));
        }

        // Price range on the default-list price of the default variation.
        if (query.MinPrice.HasValue)
            products = products.Where(p => p.Variations
                .Where(v => v.IsDefault && !v.IsDeleted)
                .SelectMany(v => db.PriceListItems
                    .Where(i => i.VariationId == v.Id && db.PriceLists.Any(l => l.Id == i.PriceListId && l.IsDefault && l.OwnerId == null))
                    .Select(i => (decimal?)i.Price))
                .FirstOrDefault() >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue)
            products = products.Where(p => p.Variations
                .Where(v => v.IsDefault && !v.IsDeleted)
                .SelectMany(v => db.PriceListItems
                    .Where(i => i.VariationId == v.Id && db.PriceLists.Any(l => l.Id == i.PriceListId && l.IsDefault && l.OwnerId == null))
                    .Select(i => (decimal?)i.Price))
                .FirstOrDefault() <= query.MaxPrice.Value);

        products = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"        => products.OrderBy(p => p.Name),
            "-name"       => products.OrderByDescending(p => p.Name),
            "createdat" or "createdatutc"   => products.OrderBy(p => p.CreatedAtUtc),
            "-createdat" or "-createdatutc" => products.OrderByDescending(p => p.CreatedAtUtc),
            "updatedat" or "updatedatutc"   => products.OrderBy(p => p.UpdatedAtUtc),
            "-updatedat" or "-updatedatutc" => products.OrderByDescending(p => p.UpdatedAtUtc),
            "primarycategoryname"  => products.OrderBy(p => db.Set<ProductCategory>().Where(pc => pc.ProductId == p.Id && pc.IsPrimary).Join(db.Categories, pc => pc.CategoryId, c => c.Id, (pc, c) => c.Name).FirstOrDefault()),
            "-primarycategoryname" => products.OrderByDescending(p => db.Set<ProductCategory>().Where(pc => pc.ProductId == p.Id && pc.IsPrimary).Join(db.Categories, pc => pc.CategoryId, c => c.Id, (pc, c) => c.Name).FirstOrDefault()),
            "status"      => products.OrderBy(p => p.Status),
            "-status"     => products.OrderByDescending(p => p.Status),
            "type"        => products.OrderBy(p => p.Type),
            "-type"       => products.OrderByDescending(p => p.Type),
            "slug"        => products.OrderBy(p => p.Slug),
            "-slug"       => products.OrderByDescending(p => p.Slug),
            "brandname"   => products.OrderBy(p => db.Brands.Where(b => b.Id == p.BrandId).Select(b => b.Name).FirstOrDefault()),
            "-brandname"  => products.OrderByDescending(p => db.Brands.Where(b => b.Id == p.BrandId).Select(b => b.Name).FirstOrDefault()),
            "defaultsku"  => products.OrderBy(p => p.Variations.Where(v => v.IsDefault && !v.IsDeleted).Select(v => v.Sku).FirstOrDefault()),
            "-defaultsku" => products.OrderByDescending(p => p.Variations.Where(v => v.IsDefault && !v.IsDeleted).Select(v => v.Sku).FirstOrDefault()),
            "defaultprice"  => products.OrderBy(p => p.Variations
                .Where(v => v.IsDefault && !v.IsDeleted)
                .SelectMany(v => db.PriceListItems
                    .Where(i => i.VariationId == v.Id && db.PriceLists.Any(l => l.Id == i.PriceListId && l.IsDefault && l.OwnerId == null))
                    .Select(i => (decimal?)i.Price))
                .FirstOrDefault()),
            "-defaultprice" => products.OrderByDescending(p => p.Variations
                .Where(v => v.IsDefault && !v.IsDeleted)
                .SelectMany(v => db.PriceListItems
                    .Where(i => i.VariationId == v.Id && db.PriceLists.Any(l => l.Id == i.PriceListId && l.IsDefault && l.OwnerId == null))
                    .Select(i => (decimal?)i.Price))
                .FirstOrDefault()),
            _             => products.OrderBy(p => p.Name),
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
                p.UpdatedAtUtc,
                p.Variations
                    .Where(v => v.IsDefault && !v.IsDeleted)
                    .SelectMany(v => db.PriceListItems
                        .Where(i => i.VariationId == v.Id && db.PriceLists.Any(l => l.Id == i.PriceListId && l.IsDefault && l.OwnerId == null))
                        .Select(i => (decimal?)i.Price))
                    .FirstOrDefault(),
                db.ProductCodes
                    .Where(c => c.ProductId == p.Id)
                    .OrderBy(c => c.CodeType)
                    .Select(c => new ProductCodeBriefDto(c.CodeType, c.Code))
                    .ToList()))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
