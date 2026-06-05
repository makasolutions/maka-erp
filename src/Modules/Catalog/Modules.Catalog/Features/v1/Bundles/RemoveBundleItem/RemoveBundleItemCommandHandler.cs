using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Bundles.RemoveBundleItem;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Bundles.RemoveBundleItem;

public sealed class RemoveBundleItemCommandHandler(CatalogDbContext db)
    : ICommandHandler<RemoveBundleItemCommand, Guid>
{
    public async ValueTask<Guid> Handle(RemoveBundleItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await db.Set<ProductBundleItem>()
            .Where(b => b.Id == command.ItemId && b.ProductId == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Bundle item {command.ItemId} not found.");

        db.Set<ProductBundleItem>().Remove(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id;
    }
}
