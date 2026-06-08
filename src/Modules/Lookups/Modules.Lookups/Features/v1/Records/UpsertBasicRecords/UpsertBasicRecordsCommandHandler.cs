using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.Records.UpsertBasicRecords;
using FSH.Modules.Lookups.Data;
using FSH.Modules.Lookups.Domain;
using FSH.Modules.Lookups.Features;
using Mediator;

namespace FSH.Modules.Lookups.Features.v1.Records.UpsertBasicRecords;

public sealed class UpsertBasicRecordsCommandHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<UpsertBasicRecordsCommand>
{
    public async ValueTask<Unit> Handle(UpsertBasicRecordsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (tenantId, isRoot) = LookupGuards.Tenant(tenantAccessor);

        var table = await LookupGuards.LoadEditableAsync(db, command.Id, tenantId, isRoot, cancellationToken).ConfigureAwait(false);

        var originalIds = table.Records.Select(r => r.Id).ToList();
        var keepIds = new HashSet<Guid>();

        foreach (var input in command.Records)
        {
            var existing = input.Id is Guid gid ? table.Records.FirstOrDefault(r => r.Id == gid) : null;
            if (existing is not null)
            {
                existing.Update(input.Value, input.SortOrder, input.IsActive);
                keepIds.Add(existing.Id);
            }
            else
            {
                db.BasicRecords.Add(BasicRecord.Create(
                    table.Id, input.Code, input.Value, table.TenantId, input.SortOrder, input.IsActive));
            }
        }

        var removed = table.Records.Where(r => originalIds.Contains(r.Id) && !keepIds.Contains(r.Id)).ToList();
        if (removed.Count > 0) db.BasicRecords.RemoveRange(removed);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
