using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.GetPublicProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.GetPublicProduct;

public sealed class GetPublicProductQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPublicProductQuery, PublicProductDto>
{
    public async ValueTask<PublicProductDto> Handle(GetPublicProductQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var slug = query.Slug.Trim().ToLowerInvariant();

        var p = await db.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Variations.Where(v => !v.IsDeleted)).ThenInclude(v => v.AttributeValues)
            .Where(x => !x.IsDeleted && x.Slug == slug && x.Status == ProductStatus.Active)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Product not found.");

        string? brandName = p.BrandId.HasValue
            ? await db.Brands.AsNoTracking().Where(b => b.Id == p.BrandId.Value).Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            : null;

        // Codes of the default variation (Simple/Service); for Variable, none at product level.
        var defaultVar = p.Variations.FirstOrDefault(v => v.IsDefault);
        var codes = new List<PublicCodeDto>();
        if (defaultVar is not null)
        {
            codes.Add(new PublicCodeDto("SKU", defaultVar.Sku));
            var extra = await db.Set<ProductCode>().AsNoTracking()
                .Where(c => c.VariationId == defaultVar.Id && c.CodeType != "SKU")
                .Select(c => new PublicCodeDto(c.CodeType, c.Code))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            codes.AddRange(extra);
        }

        // Resolve attribute names for variation combinations.
        var attrIds = p.Variations.SelectMany(v => v.AttributeValues.Select(av => av.AttributeId)).Distinct().ToList();
        var attrNames = attrIds.Count > 0
            ? await db.Attributes.AsNoTracking().Where(a => attrIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken).ConfigureAwait(false)
            : [];

        var variations = p.Variations
            .OrderByDescending(v => v.IsDefault).ThenBy(v => v.Sku)
            .Select(v => new PublicVariationDto(
                v.Sku,
                string.Join(", ", v.AttributeValues.Select(av => $"{attrNames.GetValueOrDefault(av.AttributeId, string.Empty)}: {av.Value}"))))
            .ToList();

        return new PublicProductDto(
            p.Id, p.Name, p.Slug, brandName, p.Type,
            p.ShortDescription, p.Description, p.TechnicalSpecs,
            p.Specs != null ? p.Specs.RootElement.GetRawText() : null,
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => new PublicImageDto(i.Url, i.AltText, i.IsPrimary, i.SortOrder)).ToList(),
            codes,
            variations);
    }
}
