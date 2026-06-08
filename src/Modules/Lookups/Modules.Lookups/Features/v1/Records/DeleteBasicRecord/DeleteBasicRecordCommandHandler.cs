using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Contracts.v1.Records.DeleteBasicRecord;
using FSH.Modules.Lookups.Data;
using Mediator;

namespace FSH.Modules.Lookups.Features.v1.Records.DeleteBasicRecord;

public sealed class DeleteBasicRecordCommandHandler(
    LookupsDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<DeleteBasicRecordCommand>
{
    public async ValueTask<Unit> Handle(DeleteBasicRecordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var (tenantId, isRoot) = LookupGuards.Tenant(tenantAccessor);

        var table = await LookupGuards.LoadEditableAsync(db, command.TableId, tenantId, isRoot, cancellationToken).ConfigureAwait(false);
        var record = table.Records.FirstOrDefault(r => r.Id == command.RecordId)
            ?? throw new CustomException("Registro no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        db.BasicRecords.Remove(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
