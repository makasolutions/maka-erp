using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.DeleteProduct;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.DeleteProduct;

public sealed class DeleteProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Where(p => !p.IsDeleted && p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.Id} not found.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
