using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.GetShippingClasses;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.GetShippingClasses;

public sealed class GetShippingClassesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetShippingClassesQuery, IReadOnlyList<ShippingClassDto>>
{
    public async ValueTask<IReadOnlyList<ShippingClassDto>> Handle(GetShippingClassesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.ShippingClasses
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ShippingClassDto(s.Id, s.Name, s.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
