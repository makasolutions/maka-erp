using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTableById;
using FSH.Modules.Lookups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTableById;

public sealed class GetBasicTableByIdQueryHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetBasicTableByIdQuery, BasicTableDetailDto>
{
    public async ValueTask<BasicTableDetailDto> Handle(GetBasicTableByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (tenantId, _) = LookupGuards.Tenant(tenantAccessor);

        var table = await db.BasicTables
            .AsNoTracking()
            .Where(t => t.Id == query.Id && !t.IsDeleted && (t.TenantId == null || t.TenantId == tenantId))
            .Select(t => new BasicTableDetailDto(
                t.Id, t.Code, t.Name, t.Description, t.IsManageable, t.SortOrder, t.VisibleInMenu, t.IsGlobal,
                t.Records.OrderBy(r => r.SortOrder).ThenBy(r => r.Value)
                    .Select(r => new BasicRecordDto(r.Id, r.Code, r.Value, r.SortOrder, r.IsActive)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return table ?? throw new CustomException("Tabla básica no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
    }
}
