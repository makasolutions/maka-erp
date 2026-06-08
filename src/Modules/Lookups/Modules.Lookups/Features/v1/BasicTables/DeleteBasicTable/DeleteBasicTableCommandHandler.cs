using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.DeleteBasicTable;
using FSH.Modules.Lookups.Data;
using Mediator;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.DeleteBasicTable;

public sealed class DeleteBasicTableCommandHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<DeleteBasicTableCommand>
{
    public async ValueTask<Unit> Handle(DeleteBasicTableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (tenantId, isRoot) = LookupGuards.Tenant(tenantAccessor);

        var table = await LookupGuards.LoadEditableAsync(db, command.Id, tenantId, isRoot, cancellationToken).ConfigureAwait(false);
        db.BasicTables.Remove(table); // soft-delete via auditing interceptor (ISoftDeletable)
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
