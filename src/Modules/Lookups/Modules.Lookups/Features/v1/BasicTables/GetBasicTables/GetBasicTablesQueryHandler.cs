using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Lookups.Contracts.v1.BasicTables;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTables;
using FSH.Modules.Lookups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTables;

public sealed class GetBasicTablesQueryHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetBasicTablesQuery, PagedResponse<BasicTableDto>>
{
    public async ValueTask<PagedResponse<BasicTableDto>> Handle(GetBasicTablesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (tenantId, _) = LookupGuards.Tenant(tenantAccessor);

        // Visibilidad: globales (TenantId null) + las del tenant actual.
        var tables = db.BasicTables
            .AsNoTracking()
            .Where(t => !t.IsDeleted && (t.TenantId == null || t.TenantId == tenantId));

        if (query.IsGlobal.HasValue)
            tables = tables.Where(t => t.IsGlobal == query.IsGlobal.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search}%";
            tables = tables.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Code, pattern));
        }

        tables = (query.Sort?.ToLowerInvariant()) switch
        {
            "name"  => tables.OrderBy(t => t.Name),
            "-name" => tables.OrderByDescending(t => t.Name),
            "code"  => tables.OrderBy(t => t.Code),
            _       => tables.OrderBy(t => t.SortOrder).ThenBy(t => t.Name),
        };

        return await tables
            .Select(t => new BasicTableDto(
                t.Id, t.Code, t.Name, t.Description, t.IsManageable, t.SortOrder,
                t.VisibleInMenu, t.IsGlobal, t.Records.Count, t.CreatedAtUtc))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
