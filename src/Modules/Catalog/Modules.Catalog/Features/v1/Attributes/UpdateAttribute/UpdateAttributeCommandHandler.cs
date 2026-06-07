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

        // Replace the category associations when the caller provides a list
        // (null = leave untouched; empty = clear all).
        if (command.CategoryIds is not null)
        {
            var existing = await db.CategoryAttributes
                .Where(ca => ca.AttributeId == command.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            db.CategoryAttributes.RemoveRange(existing);

            int order = 0;
            foreach (var categoryId in command.CategoryIds.Distinct())
                db.CategoryAttributes.Add(Domain.CategoryAttribute.Create(categoryId, command.Id, order++));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attribute.Id;
    }
}
