using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Categories.ListTrashedCategories;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;

public sealed class ListTrashedCategoriesQueryHandler(CatalogDbContext db)
    : IQueryHandler<ListTrashedCategoriesQuery, PagedResponse<TrashedCategoryDto>>
{
    public async ValueTask<PagedResponse<TrashedCategoryDto>> Handle(
        ListTrashedCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var categories = db.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            categories = categories.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                EF.Functions.ILike(c.Slug, pattern));
        }

        categories = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"      => categories.OrderBy(c => c.Name),
            "-name"     => categories.OrderByDescending(c => c.Name),
            "deletedat" => categories.OrderBy(c => c.DeletedOnUtc),
            _           => categories.OrderByDescending(c => c.DeletedOnUtc),
        };

        return await categories
            .Select(c => new TrashedCategoryDto(
                c.Id,
                c.ParentId,
                c.Name,
                c.Slug,
                c.IsActive,
                c.DeletedOnUtc,
                c.DeletedBy))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
