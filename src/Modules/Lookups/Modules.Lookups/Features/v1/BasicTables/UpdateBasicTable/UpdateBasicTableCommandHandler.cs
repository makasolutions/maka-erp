using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.UpdateBasicTable;
using FSH.Modules.Lookups.Data;
using Mediator;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.UpdateBasicTable;

public sealed class UpdateBasicTableCommandHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<UpdateBasicTableCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateBasicTableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (tenantId, isRoot) = LookupGuards.Tenant(tenantAccessor);

        var table = await LookupGuards.LoadEditableAsync(db, command.Id, tenantId, isRoot, cancellationToken).ConfigureAwait(false);
        table.Update(command.Name, command.Description, command.IsManageable, command.SortOrder, command.VisibleInMenu);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return table.Id;
    }
}
