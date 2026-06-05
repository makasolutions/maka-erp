using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Bundles.AddBundleItem;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Bundles.AddBundleItem;

public sealed class AddBundleItemCommandHandler(CatalogDbContext db)
    : ICommandHandler<AddBundleItemCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddBundleItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == command.ProductId)
            .Select(p => new { p.Id, p.Type })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        if (product.Type != ProductType.Bundle)
            throw new CustomException(
                "Solo los productos de tipo Bundle pueden tener items de combo.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        bool variationExists = await db.Variations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(v => v.Id == command.ItemVariationId && !v.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (!variationExists)
            throw new NotFoundException($"Variation {command.ItemVariationId} not found.");

        var item = ProductBundleItem.Create(
            command.ProductId,
            command.ItemVariationId,
            command.Quantity,
            command.DiscountPercent,
            command.DiscountFixed,
            command.IsOptional,
            command.SortOrder);

        db.Set<ProductBundleItem>().Add(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id;
    }
}
