using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.Records.GetBasicRecordsByCode;
using FSH.Modules.Lookups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features.v1.Records.GetBasicRecordsByCode;

public sealed class GetBasicRecordsByCodeQueryHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetBasicRecordsByCodeQuery, IReadOnlyList<BasicRecordBriefDto>>
{
    public async ValueTask<IReadOnlyList<BasicRecordBriefDto>> Handle(GetBasicRecordsByCodeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (tenantId, _) = LookupGuards.Tenant(tenantAccessor);
        string code = query.TableCode.Trim();

        // Visible: la tabla del tenant o la global. Si existen ambas, gana la del tenant.
        var table = await db.BasicTables
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Code == code && (t.TenantId == null || t.TenantId == tenantId))
            .OrderByDescending(t => t.TenantId != null)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (table is null) return [];

        return await db.BasicRecords
            .AsNoTracking()
            .Where(r => r.BasicTableId == table.Id && r.IsActive)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Value)
            .Select(r => new BasicRecordBriefDto(r.Code, r.Value, r.SortOrder))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
