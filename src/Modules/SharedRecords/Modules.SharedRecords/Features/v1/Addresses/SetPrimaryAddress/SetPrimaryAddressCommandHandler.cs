using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.SetPrimaryAddress;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.SetPrimaryAddress;

public sealed class SetPrimaryAddressCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<SetPrimaryAddressCommand>
{
    public async ValueTask<Unit> Handle(SetPrimaryAddressCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var target = await db.Addresses.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Dirección no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        await AddressPrimaryWriter.MakePrimaryAsync(db, target, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
