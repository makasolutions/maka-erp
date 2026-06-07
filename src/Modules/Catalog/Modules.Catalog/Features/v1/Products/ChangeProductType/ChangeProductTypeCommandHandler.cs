using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.ChangeProductType;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.ChangeProductType;

public sealed class ChangeProductTypeCommandHandler(CatalogDbContext db)
    : ICommandHandler<ChangeProductTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(ChangeProductTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Where(p => p.Id == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        // Variable → Simple is only safe with a single (default) variation.
        if (product.Type is ProductType.Variable && command.NewType is ProductType.Simple)
        {
            int variations = await db.Variations
                .IgnoreQueryFilters()
                .CountAsync(v => v.ProductId == product.Id && !v.IsDeleted, cancellationToken)
                .ConfigureAwait(false);
            if (variations > 1)
                throw new CustomException(
                    "No se puede convertir a Simple: el producto tiene más de una variación. Elimina las variaciones extra primero.",
                    Enumerable.Empty<string>(),
                    HttpStatusCode.BadRequest);
        }

        product.ChangeType(command.NewType);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
