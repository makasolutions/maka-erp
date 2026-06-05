using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributes;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.GetAttributes;

public sealed class GetAttributesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetAttributesQuery, PagedResponse<AttributeDto>>
{
    public async ValueTask<PagedResponse<AttributeDto>> Handle(GetAttributesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var attributes = db.Attributes.AsNoTracking();

        if (query.IsUsedForVariations.HasValue)
        {
            attributes = attributes.Where(a => a.IsUsedForVariations == query.IsUsedForVariations.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            attributes = attributes.Where(a =>
                EF.Functions.ILike(a.Name, pattern) ||
                EF.Functions.ILike(a.Slug, pattern));
        }

        attributes = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"      => attributes.OrderBy(a => a.Name),
            "-name"     => attributes.OrderByDescending(a => a.Name),
            "sortorder" => attributes.OrderBy(a => a.SortOrder).ThenBy(a => a.Name),
            _           => attributes.OrderBy(a => a.SortOrder).ThenBy(a => a.Name),
        };

        return await attributes
            .Select(a => new AttributeDto(
                a.Id,
                a.Name,
                a.Slug,
                a.Type,
                a.IsVisibleOnProduct,
                a.IsUsedForVariations,
                a.SortOrder,
                a.Values.Count,
                a.WooCommerceId))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
