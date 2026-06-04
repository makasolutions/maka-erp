using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.CreateProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string slug = SlugHelper.Build(command.Slug, command.Name);

        bool slugExists = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Slug == slug)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        if (command.BrandId.HasValue)
        {
            bool brandExists = await db.Brands
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.Id == command.BrandId.Value)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!brandExists)
                throw new NotFoundException($"Brand {command.BrandId.Value} not found.");
        }

        // Validate SKU uniqueness when provided (global — across tenants filtered by EF global query filter)
        if (!string.IsNullOrWhiteSpace(command.DefaultSku))
        {
            string normalizedSku = command.DefaultSku.Trim().ToUpperInvariant();
            bool skuExists = await db.Variations
                .IgnoreQueryFilters()
                .Where(v => !v.IsDeleted && v.Sku == normalizedSku)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (skuExists)
                throw new CustomException("El SKU ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }

        var product = Product.Create(
            command.Name,
            slug,
            command.Type,
            command.BrandId,
            command.TaxRateId,
            command.ShippingClassId,
            command.ShortDescription,
            defaultSku: command.DefaultSku);

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
