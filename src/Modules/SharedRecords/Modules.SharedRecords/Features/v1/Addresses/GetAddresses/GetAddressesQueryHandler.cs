using FSH.Modules.SharedRecords.Contracts.v1.Addresses;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.GetAddresses;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.GetAddresses;

public sealed class GetAddressesQueryHandler(SharedRecordsDbContext db)
    : IQueryHandler<GetAddressesQuery, IReadOnlyList<AddressDto>>
{
    public async ValueTask<IReadOnlyList<AddressDto>> Handle(GetAddressesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var ownerType = query.OwnerType?.Trim() ?? string.Empty;

        // Scoped al owner. El filtro global por TenantId (BaseDbContext) garantiza el aislamiento.
        var items = await db.Addresses.AsNoTracking()
            .Where(a => a.OwnerType == ownerType && a.OwnerId == query.OwnerId)
            .OrderByDescending(a => a.IsPrimary).ThenByDescending(a => a.IsActive).ThenBy(a => a.CreatedOnUtc)
            .Select(a => a.ToDto())
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items;
    }
}
