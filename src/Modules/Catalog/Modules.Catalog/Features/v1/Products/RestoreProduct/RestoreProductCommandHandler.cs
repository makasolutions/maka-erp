using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.RestoreProduct;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.RestoreProduct;

public sealed class RestoreProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<RestoreProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted && p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.Id} not found in trash.");

        product.Restore();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
