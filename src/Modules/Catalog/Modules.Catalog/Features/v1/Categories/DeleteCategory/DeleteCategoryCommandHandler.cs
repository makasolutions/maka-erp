using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories.DeleteCategory;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await db.Categories
            .Where(c => !c.IsDeleted && c.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Category {command.Id} not found.");

        bool hasChildren = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.ParentId == command.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hasChildren)
            throw new CustomException(
                "No se puede eliminar la categoría porque tiene sub-categorías activas.",
                Enumerable.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);

        bool hasProducts = await db.Set<FSH.Modules.Catalog.Domain.ProductCategory>()
            .AsNoTracking()
            .Where(pc => pc.CategoryId == command.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hasProducts)
            throw new CustomException(
                "No se puede eliminar la categoría porque tiene productos activos asociados.",
                Enumerable.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Id;
    }
}
