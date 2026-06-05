using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.DeleteAttribute;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.DeleteAttribute;

public sealed class DeleteAttributeCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteAttributeCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteAttributeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await db.Attributes
            .Include(a => a.Values)
            .Where(a => a.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Attribute {command.Id} not found.");

        bool inUse = await db.Set<ProductAttribute>()
            .AsNoTracking()
            .AnyAsync(pa => pa.AttributeId == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
            throw new CustomException(
                "No se puede eliminar el atributo porque está asignado a uno o más productos.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        // Values cascade-delete via the configured FK relationship.
        db.Attributes.Remove(attribute);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attribute.Id;
    }
}
