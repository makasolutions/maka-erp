using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttribute;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttribute;

public sealed class UpdateAttributeCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateAttributeCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateAttributeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attribute = await db.Attributes
            .Where(a => a.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Attribute {command.Id} not found.");

        attribute.Update(
            command.Name,
            command.Type,
            command.IsVisibleOnProduct,
            command.IsUsedForVariations,
            command.SortOrder);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attribute.Id;
    }
}
