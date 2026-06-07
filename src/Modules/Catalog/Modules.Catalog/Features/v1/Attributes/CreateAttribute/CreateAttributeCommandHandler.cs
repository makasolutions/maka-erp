using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.CreateAttribute;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.CreateAttribute;

public sealed class CreateAttributeCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateAttributeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAttributeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string slug = SlugHelper.Build(command.Slug, command.Name);

        bool slugExists = await db.Attributes
            .AsNoTracking()
            .AnyAsync(a => a.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug del atributo ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var attribute = CatalogAttribute.Create(
            command.Name,
            slug,
            command.Type,
            command.IsVisibleOnProduct,
            command.IsUsedForVariations,
            command.SortOrder);

        db.Attributes.Add(attribute);

        if (command.CategoryIds is { Count: > 0 })
        {
            int order = 0;
            foreach (var categoryId in command.CategoryIds.Distinct())
                db.CategoryAttributes.Add(CategoryAttribute.Create(categoryId, attribute.Id, order++));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return attribute.Id;
    }
}
