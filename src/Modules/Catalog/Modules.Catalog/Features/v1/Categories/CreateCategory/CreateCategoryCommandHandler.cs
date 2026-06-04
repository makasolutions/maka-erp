using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories.CreateCategory;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ParentId.HasValue)
        {
            bool parentExists = await db.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.Id == command.ParentId.Value)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
                throw new NotFoundException($"Parent category {command.ParentId.Value} not found.");
        }

        string slug = SlugHelper.Build(command.Slug, command.Name);

        bool slugExists = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Slug == slug)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var category = Category.Create(
            command.Name,
            slug,
            command.ParentId,
            command.Description,
            command.ImageUrl,
            command.SortOrder,
            isActive: command.IsActive);

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Id;
    }
}
