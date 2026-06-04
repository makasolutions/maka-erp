using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories.GetCategoryById;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetCategoryByIdQuery, CategoryDetailDto>
{
    public async ValueTask<CategoryDetailDto> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var category = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == query.Id)
            .Select(c => new CategoryDetailDto(
                c.Id,
                c.ParentId,
                c.Name,
                c.Slug,
                c.Description,
                c.ImageUrl,
                c.SortOrder,
                c.IsActive,
                c.WooCommerceId,
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return category ?? throw new NotFoundException($"Category {query.Id} not found.");
    }
}
