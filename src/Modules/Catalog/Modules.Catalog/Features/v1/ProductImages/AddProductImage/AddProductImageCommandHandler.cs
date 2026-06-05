using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;

public sealed class AddProductImageCommandHandler(CatalogDbContext db)
    : ICommandHandler<AddProductImageCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddProductImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool productExists = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {command.ProductId} not found.");

        var existing = await db.Set<ProductImage>()
            .Where(i => i.ProductId == command.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // First image of a product is always primary; an explicit primary clears the others.
        bool makePrimary = command.IsPrimary || existing.Count == 0;
        if (makePrimary)
        {
            foreach (var img in existing.Where(i => i.IsPrimary))
                img.SetPrimary(false);
        }

        var image = ProductImage.Create(
            command.ProductId,
            command.Url,
            command.AltText,
            makePrimary,
            command.SortOrder);

        db.Set<ProductImage>().Add(image);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return image.Id;
    }
}
