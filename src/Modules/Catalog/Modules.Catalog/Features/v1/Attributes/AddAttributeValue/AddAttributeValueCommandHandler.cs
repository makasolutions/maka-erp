using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.AddAttributeValue;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.AddAttributeValue;

public sealed class AddAttributeValueCommandHandler(CatalogDbContext db)
    : ICommandHandler<AddAttributeValueCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddAttributeValueCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool attributeExists = await db.Attributes
            .AsNoTracking()
            .AnyAsync(a => a.Id == command.AttributeId, cancellationToken)
            .ConfigureAwait(false);

        if (!attributeExists)
            throw new NotFoundException($"Attribute {command.AttributeId} not found.");

        string value = command.Value.Trim();

        bool duplicate = await db.Set<CatalogAttributeValue>()
            .AsNoTracking()
            .AnyAsync(v => v.AttributeId == command.AttributeId && EF.Functions.ILike(v.Value, value), cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            throw new CustomException(
                "Ya existe un valor con ese nombre para este atributo.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var attributeValue = CatalogAttributeValue.Create(
            command.AttributeId,
            value,
            command.ColorCode,
            command.ImageUrl,
            command.SortOrder);

        db.Set<CatalogAttributeValue>().Add(attributeValue);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attributeValue.Id;
    }
}
