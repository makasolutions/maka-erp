using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.GetResolvedProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.GetResolvedProduct;

public sealed class GetResolvedProductQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetResolvedProductQuery, ResolvedProductDto>
{
    public async ValueTask<ResolvedProductDto> Handle(GetResolvedProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var canonical = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == query.CanonicalProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {query.CanonicalProductId} not found.");

        var ov = await db.Set<TenantProduct>()
            .AsNoTracking()
            .Where(tp => tp.CanonicalProductId == query.CanonicalProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Each effective field = override when present, else the canonical value.
        return new ResolvedProductDto(
            CanonicalProductId: canonical.Id,
            TenantProductId:    ov?.Id,
            HasOverride:        ov is not null,
            Name:               ov?.NameOverride ?? canonical.Name,
            ShortDescription:   ov?.ShortDescriptionOverride ?? canonical.ShortDescription,
            Description:        ov?.DescriptionOverride ?? canonical.Description,
            TechnicalSpecs:     ov?.TechnicalSpecsOverride ?? canonical.TechnicalSpecs,
            SeoTitle:           ov?.SeoTitleOverride ?? canonical.SeoTitle,
            SeoDescription:     ov?.SeoDescriptionOverride ?? canonical.SeoDescription,
            DropshippingPrice:  ov?.DropshippingPrice,
            DropshippingMinQty: ov?.DropshippingMinQty,
            IsActive:           ov?.IsActive ?? (canonical.Status == Contracts.Enums.ProductStatus.Active),
            IsPublic:           ov?.IsPublic ?? canonical.IsPublic);
    }
}
