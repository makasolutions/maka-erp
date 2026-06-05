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

        var variations = await db.Variations
            .IgnoreQueryFilters()   // include soft-deleted so UI can show them
            .AsNoTracking()
            .Include(v => v.AttributeValues)
            .Where(v => v.ProductId == query.ProductId)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Resolve attribute names for the values referenced by these variations.
        var attributeIds = variations
            .SelectMany(v => v.AttributeValues.Select(av => av.AttributeId))
            .Distinct()
            .ToList();

        var attributeNames = attributeIds.Count > 0
            ? await db.Attributes
                .AsNoTracking()
                .Where(a => attributeIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken)
                .ConfigureAwait(false)
            : [];

        return variations
            .Select(v => new VariationDto(
                v.Id,
                v.ProductId,
                v.Sku,
                v.Description,
                v.IsDefault,
                v.IsActive,
                v.IsDeleted,
                v.Weight,
                v.WeightUnit?.ToString(),
                v.ImageUrl,
                v.ManageStock,
                v.AllowBackorders,
                v.SoldIndividually,
                v.LowStockThreshold,
                v.IsVirtual,
                v.WooCommerceId,
                v.CreatedAtUtc,
                v.UpdatedAtUtc,
                v.AttributeValues
                    .Select(av => new VariationAttributeValueDto(
                        av.AttributeId,
                        attributeNames.GetValueOrDefault(av.AttributeId, string.Empty),
                        av.Id,
                        av.Value,
                        av.ColorCode))
                    .OrderBy(x => x.AttributeName)
                    .ToList()))
            .ToList();
    }
}
