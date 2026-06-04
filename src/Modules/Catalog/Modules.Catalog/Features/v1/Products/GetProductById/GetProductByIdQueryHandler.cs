using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.GetProductById;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductById;

public sealed class GetProductByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductByIdQuery, ProductDetailDto>
{
    public async ValueTask<ProductDetailDto> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var p = await db.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Variations.Where(v => !v.IsDeleted))
            .Include(x => x.ProductCategories)
            .Include(x => x.Tags)
            .Where(x => !x.IsDeleted && x.Id == query.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {query.Id} not found.");

        string? brandName = p.BrandId.HasValue
            ? await db.Brands.AsNoTracking()
                .Where(b => b.Id == p.BrandId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;

        var categoryIds = p.ProductCategories.Select(pc => pc.CategoryId).ToList();
        var categories = categoryIds.Count == 0
            ? []
            : await db.Categories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.Slug })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return new ProductDetailDto(
            p.Id,
            p.Name,
            p.Slug,
            p.ShortDescription,
            p.Description,
            p.TechnicalSpecs,
            p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
            p.BrandId,
            brandName,
            p.TaxRateId,
            p.ShippingClassId,
            p.Type,
            p.Status,
            p.IsVirtual,
            p.IsDownloadable,
            p.IsPublic,
            p.Weight,
            p.WeightUnit.ToString(),
            p.DimensionLength,
            p.DimensionWidth,
            p.DimensionHeight,
            p.DimensionUnit.ToString(),
            p.SeoTitle,
            p.SeoDescription,
            p.SeoKeywords,
            p.WooCommerceId,
            p.CreatedAtUtc,
            p.UpdatedAtUtc,
            p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList(),
            p.Variations.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Sku)
                .Select(v => new ProductVariationSummaryDto(v.Id, v.Sku, v.Description, v.IsDefault, v.IsActive)).ToList(),
            categories.Select(c => new ProductCategoryDto(c.Id, c.Name, c.Slug, p.ProductCategories.Any(pc => pc.CategoryId == c.Id && pc.IsPrimary))).ToList(),
            p.Tags.Select(t => t.Name).ToList());
    }
}
