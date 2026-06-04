using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories.RestoreCategory;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.RestoreCategory;

public sealed class RestoreCategoryCommandHandler(CatalogDbContext db)
    : ICommandHandler<RestoreCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted && c.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Category {command.Id} not found in trash.");

        category.Restore();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return category.Id;
    }
}
