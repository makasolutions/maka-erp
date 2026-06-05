using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributeById;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Attributes.GetAttributeById;

public sealed class GetAttributeByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetAttributeByIdQuery, AttributeDetailDto>
{
    public async ValueTask<AttributeDetailDto> Handle(GetAttributeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var attribute = await db.Attributes
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new AttributeDetailDto(
                a.Id,
                a.Name,
                a.Slug,
                a.Type,
                a.IsVisibleOnProduct,
                a.IsUsedForVariations,
                a.SortOrder,
                a.WooCommerceId,
                a.CreatedAtUtc,
                a.UpdatedAtUtc,
                a.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.Value)
                    .Select(v => new AttributeValueDto(
                        v.Id,
                        v.AttributeId,
                        v.Value,
                        v.ColorCode,
                        v.ImageUrl,
                        v.SortOrder,
                        v.WooCommerceId))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return attribute ?? throw new NotFoundException($"Attribute {query.Id} not found.");
    }
}
