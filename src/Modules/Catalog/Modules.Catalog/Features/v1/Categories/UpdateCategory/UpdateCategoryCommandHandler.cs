using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories.UpdateCategory;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await db.Categories
            .Where(c => !c.IsDeleted && c.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Category {command.Id} not found.");

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
            .Where(c => !c.IsDeleted && c.Slug == slug && c.Id != command.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        category.Update(
            command.Name,
            slug,
            command.ParentId,
            command.Description,
            command.ImageUrl,
            command.SortOrder,
            command.IsActive);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Id;
    }
}
