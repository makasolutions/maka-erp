using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.PublishProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.PublishProduct;

public sealed class PublishProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<PublishProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(PublishProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Include(p => p.Variations.Where(v => !v.IsDeleted && v.IsActive))
            .Where(p => !p.IsDeleted && p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.Id} not found.");

        if (product.Status == ProductStatus.Active)
            return product.Id;

        // Simple and Service require at least one active variation
        if (product.Type is ProductType.Simple or ProductType.Service && product.Variations.Count == 0)
            throw new CustomException(
                "El producto no tiene variaciones activas. Agrega al menos una antes de publicar.",
                Enumerable.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);

        product.Publish();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
