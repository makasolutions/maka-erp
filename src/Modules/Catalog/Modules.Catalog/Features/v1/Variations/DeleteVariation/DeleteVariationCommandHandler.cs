using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Variations.DeleteVariation;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.DeleteVariation;

public sealed class DeleteVariationCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteVariationCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteVariationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var variation = await db.Variations
            .Where(v => !v.IsDeleted && v.ProductId == command.ProductId && v.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Variation {command.Id} not found for product {command.ProductId}.");

        if (variation.IsDefault)
            throw new CustomException(
                "No se puede eliminar la variación por defecto del producto. Asigna otra variación como principal primero.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        db.Variations.Remove(variation);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return variation.Id;
    }
}
