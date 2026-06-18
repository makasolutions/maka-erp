using FSH.Modules.SharedRecords.Contracts.v1.Phones;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.GetPhones;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.GetPhones;

public sealed class GetPhonesQueryHandler(SharedRecordsDbContext db)
    : IQueryHandler<GetPhonesQuery, IReadOnlyList<PhoneDto>>
{
    public async ValueTask<IReadOnlyList<PhoneDto>> Handle(GetPhonesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var ownerType = query.OwnerType?.Trim() ?? string.Empty;

        // Scoped al owner. El filtro global por TenantId (BaseDbContext) garantiza el aislamiento.
        var items = await db.Phones.AsNoTracking()
            .Where(p => p.OwnerType == ownerType && p.OwnerId == query.OwnerId)
            .OrderByDescending(p => p.IsPrimary).ThenByDescending(p => p.IsActive).ThenBy(p => p.CreatedOnUtc)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items;
    }
}
