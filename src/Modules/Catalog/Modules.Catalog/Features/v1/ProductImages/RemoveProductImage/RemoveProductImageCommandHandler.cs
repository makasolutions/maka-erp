using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.RemoveProductImage;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.RemoveProductImage;

public sealed class RemoveProductImageCommandHandler(CatalogDbContext db)
    : ICommandHandler<RemoveProductImageCommand, Guid>
{
    public async ValueTask<Guid> Handle(RemoveProductImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var image = await db.Set<ProductImage>()
            .Where(i => i.Id == command.ImageId && i.ProductId == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Image {command.ImageId} not found.");

        db.Set<ProductImage>().Remove(image);

        // If we removed the primary image, promote the next one (by sort/created order).
        if (image.IsPrimary)
        {
            var next = await db.Set<ProductImage>()
                .Where(i => i.ProductId == command.ProductId && i.Id != command.ImageId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            next?.SetPrimary(true);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return image.Id;
    }
}
