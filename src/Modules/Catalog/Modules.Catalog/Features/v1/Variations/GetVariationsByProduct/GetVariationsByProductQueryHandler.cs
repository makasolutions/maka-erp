using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Variations.GetVariationsByProduct;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.GetVariationsByProduct;

public sealed class GetVariationsByProductQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetVariationsByProductQuery, IReadOnlyList<VariationDto>>
{
    public async ValueTask<IReadOnlyList<VariationDto>> Handle(
        GetVariationsByProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool productExists = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == query.ProductId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {query.ProductId} not found.");

        return await db.Variations
            .IgnoreQueryFilters()   // include soft-deleted so UI can show them
            .AsNoTracking()
            .Where(v => v.ProductId == query.ProductId)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Sku)
            .Select(v => new VariationDto(
                v.Id,
                v.ProductId,
                v.Sku,
                v.Description,
                v.IsDefault,
                v.IsActive,
                v.IsDeleted,
                v.Weight,
                v.WeightUnit != null ? v.WeightUnit.ToString() : null,
                v.ImageUrl,
                v.ManageStock,
                v.AllowBackorders,
                v.SoldIndividually,
                v.LowStockThreshold,
                v.IsVirtual,
                v.WooCommerceId,
                v.CreatedAtUtc,
                v.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
