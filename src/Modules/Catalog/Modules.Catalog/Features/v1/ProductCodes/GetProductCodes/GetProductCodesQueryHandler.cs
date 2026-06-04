using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.GetProductCodes;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.GetProductCodes;

public sealed class GetProductCodesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductCodesQuery, IReadOnlyList<ProductCodeDto>>
{
    public async ValueTask<IReadOnlyList<ProductCodeDto>> Handle(
        GetProductCodesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool variationExists = await db.Variations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => v.ProductId == query.ProductId && v.Id == query.VariationId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!variationExists)
            throw new NotFoundException($"Variation {query.VariationId} not found for product {query.ProductId}.");

        return await db.ProductCodes
            .AsNoTracking()
            .Where(c => c.ProductId == query.ProductId && c.VariationId == query.VariationId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.CodeType)
            .Select(c => new ProductCodeDto(
                c.Id,
                c.VariationId,
                c.CodeType,
                c.Code,
                c.IsPrimary,
                c.SupplierId,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
