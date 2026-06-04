using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.ArchiveProduct;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.ArchiveProduct;

public sealed class ArchiveProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<ArchiveProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(ArchiveProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Where(p => !p.IsDeleted && p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.Id} not found.");

        product.Archive();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
