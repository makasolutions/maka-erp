using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.DeleteAddress;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.DeleteAddress;

public sealed class DeleteAddressCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<DeleteAddressCommand>
{
    public async ValueTask<Unit> Handle(DeleteAddressCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Dirección no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        db.Addresses.Remove(address); // soft-delete vía el interceptor de auditoría
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
