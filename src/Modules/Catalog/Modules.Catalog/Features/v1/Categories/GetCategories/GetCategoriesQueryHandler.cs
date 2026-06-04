using FSH.Modules.Catalog.Contracts.v1.Categories.GetCategories;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategories;

public sealed class GetCategoriesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async ValueTask<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var allQuery = db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (query.IsActive.HasValue)
        {
            allQuery = allQuery.Where(c => c.IsActive == query.IsActive.Value);
        }

        var all = await allQuery
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new FlatCategory(
                c.Id, c.ParentId, c.Name, c.Slug,
                c.Description, c.ImageUrl, c.SortOrder,
                c.IsActive, c.WooCommerceId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BuildTree(all, query.ParentId);
    }

    private static IReadOnlyList<CategoryDto> BuildTree(
        IEnumerable<FlatCategory> all,
        Guid? rootParentId)
    {
        // Guid? cannot be a non-null dictionary key — stringify nulls as empty string.
        var lookup = all.GroupBy(c => c.ParentId?.ToString() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Build(rootParentId);

        IReadOnlyList<CategoryDto> Build(Guid? parentId)
        {
            var key = parentId?.ToString() ?? string.Empty;
            if (!lookup.TryGetValue(key, out var children))
            {
                return [];
            }
            return children
                .Select(c => new CategoryDto(
                    c.Id, c.ParentId, c.Name, c.Slug,
                    c.Description, c.ImageUrl, c.SortOrder,
                    c.IsActive, c.WooCommerceId,
                    Build(c.Id)))
                .ToList();
        }
    }

    private sealed record FlatCategory(
        Guid    Id,
        Guid?   ParentId,
        string  Name,
        string  Slug,
        string? Description,
        string? ImageUrl,
        int     SortOrder,
        bool    IsActive,
        int?    WooCommerceId);
}
