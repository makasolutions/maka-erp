using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.DuplicateProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.DuplicateProduct;

/// <summary>
/// Deep-clones a product into a new Draft. Copies categories, tags, attributes
/// (+selected values), images, variations (with fresh unique SKUs) and bundle
/// items. Codes and prices are NOT copied (codes are cleared on the copy).
/// </summary>
public sealed class DuplicateProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<DuplicateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(DuplicateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var src = await db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .Include(p => p.ProductCategories)
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .Include(p => p.Attributes).ThenInclude(a => a.SelectedValues)
            .Include(p => p.BundleItems)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.ProductId && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        string suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        string newSlug = $"{src.Slug}-copia-{suffix.ToLowerInvariant()}";
        var srcVariations = src.Variations.Where(v => !v.IsDeleted).ToList();
        var srcDefault = srcVariations.FirstOrDefault(v => v.IsDefault) ?? srcVariations.FirstOrDefault();

        string CloneSku(string sku)
        {
            string s = $"{sku}-C{suffix}";
            return s.Length > 64 ? s[^64..] : s;
        }

        bool autoVariation = src.Type is ProductType.Simple or ProductType.Service;

        var copy = Product.Create(
            $"{src.Name} (Copia)",
            newSlug,
            src.Type,
            brandId: src.BrandId,
            taxRateId: src.TaxRateId,
            shippingClassId: src.ShippingClassId,
            shortDescription: src.ShortDescription,
            ownerId: src.OwnerId,
            defaultSku: autoVariation && srcDefault is not null ? CloneSku(srcDefault.Sku) : null);

        copy.UpdateDetails(
            copy.Name, src.ShortDescription, src.Description, src.TechnicalSpecs,
            src.BrandId, src.TaxRateId, src.ShippingClassId,
            src.IsVirtual, src.IsDownloadable, isPublic: false);

        // Variations: Simple/Service already got their default via Create; clone the
        // rest of the variation set for Variable/Bundle.
        if (!autoVariation)
        {
            foreach (var v in srcVariations)
                copy.Variations.Add(ProductVariation.Create(copy.Id, CloneSku(v.Sku), v.Description, v.IsDefault));
        }

        foreach (var pc in src.ProductCategories)
            copy.ProductCategories.Add(ProductCategory.Create(copy.Id, pc.CategoryId, pc.IsPrimary));

        foreach (var tag in src.Tags)
            copy.Tags.Add(ProductTag.Create(copy.Id, tag.Name, tag.Color));

        foreach (var img in src.Images.OrderBy(i => i.SortOrder))
            copy.Images.Add(ProductImage.Create(copy.Id, img.Url, img.AltText, img.IsPrimary, img.SortOrder));

        foreach (var attr in src.Attributes)
        {
            var pa = ProductAttribute.Create(copy.Id, attr.AttributeId, attr.IsUsedForVariations, attr.IsVisibleOnProduct, attr.SortOrder);
            pa.SetSelectedValues(attr.SelectedValues);
            copy.Attributes.Add(pa);
        }

        foreach (var bi in src.BundleItems)
            copy.BundleItems.Add(ProductBundleItem.Create(copy.Id, bi.ItemVariationId, bi.Quantity, bi.DiscountPercent, bi.DiscountFixed, bi.IsOptional, bi.SortOrder));

        db.Products.Add(copy);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return copy.Id;
    }
}
