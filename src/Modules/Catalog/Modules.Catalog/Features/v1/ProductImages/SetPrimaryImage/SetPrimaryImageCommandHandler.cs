using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.SetPrimaryImage;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.SetPrimaryImage;

public sealed class SetPrimaryImageCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetPrimaryImageCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetPrimaryImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var images = await db.Set<ProductImage>()
            .Where(i => i.ProductId == command.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var target = images.Find(i => i.Id == command.ImageId)
            ?? throw new NotFoundException($"Image {command.ImageId} not found.");

        foreach (var img in images)
            img.SetPrimary(img.Id == target.Id);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return target.Id;
    }
}
