using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.CreateBasicTable;
using FSH.Modules.Lookups.Data;
using FSH.Modules.Lookups.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.CreateBasicTable;

public sealed class CreateBasicTableCommandHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<CreateBasicTableCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateBasicTableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string? tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        bool isRoot = tenantId == MultitenancyConstants.Root.Id;

        // Solo root/operador puede crear tablas globales.
        if (command.IsGlobal && !isRoot)
            throw new CustomException("Solo el operador puede crear tablas globales.", Enumerable.Empty<string>(), HttpStatusCode.Forbidden);

        string code = command.Code.Trim();
        string? scope = command.IsGlobal ? null : tenantId;

        bool exists = await db.BasicTables
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Code == code && t.TenantId == scope)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            throw new CustomException("Ya existe una tabla básica con ese código.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var table = BasicTable.Create(
            code,
            command.Name,
            command.IsGlobal,
            tenantId,
            command.Description,
            command.IsManageable,
            command.SortOrder,
            command.VisibleInMenu);

        db.BasicTables.Add(table);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return table.Id;
    }
}
