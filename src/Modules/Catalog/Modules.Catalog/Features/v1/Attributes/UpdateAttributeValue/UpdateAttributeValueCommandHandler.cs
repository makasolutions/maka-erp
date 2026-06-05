using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttributeValue;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttributeValue;

public sealed class UpdateAttributeValueCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateAttributeValueCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateAttributeValueCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var value = await db.Set<CatalogAttributeValue>()
            .Where(v => v.Id == command.ValueId && v.AttributeId == command.AttributeId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Attribute value {command.ValueId} not found.");

        string newValue = command.Value.Trim();

        bool duplicate = await db.Set<CatalogAttributeValue>()
            .AsNoTracking()
            .AnyAsync(
                v => v.AttributeId == command.AttributeId
                  && v.Id != command.ValueId
                  && EF.Functions.ILike(v.Value, newValue),
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            throw new CustomException(
                "Ya existe un valor con ese nombre para este atributo.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        value.Update(newValue, command.ColorCode, command.ImageUrl, command.SortOrder);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return value.Id;
    }
}
