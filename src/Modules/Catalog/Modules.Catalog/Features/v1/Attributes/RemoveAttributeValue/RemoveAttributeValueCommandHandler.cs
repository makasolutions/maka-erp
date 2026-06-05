using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.RemoveAttributeValue;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.RemoveAttributeValue;

public sealed class RemoveAttributeValueCommandHandler(CatalogDbContext db)
    : ICommandHandler<RemoveAttributeValueCommand, Guid>
{
    public async ValueTask<Guid> Handle(RemoveAttributeValueCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var value = await db.Set<CatalogAttributeValue>()
            .Where(v => v.Id == command.ValueId && v.AttributeId == command.AttributeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Attribute value {command.ValueId} not found.");

        bool inUse = await db.Set<ProductAttributeValue>()
            .AsNoTracking()
            .AnyAsync(pav => pav.AttributeValueId == command.ValueId, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
            throw new CustomException(
                "No se puede eliminar el valor porque está seleccionado en uno o más productos.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        db.Set<CatalogAttributeValue>().Remove(value);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return value.Id;
    }
}
