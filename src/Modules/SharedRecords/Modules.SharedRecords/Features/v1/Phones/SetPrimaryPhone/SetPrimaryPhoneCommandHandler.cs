using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.SetPrimaryPhone;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.SetPrimaryPhone;

public sealed class SetPrimaryPhoneCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<SetPrimaryPhoneCommand>
{
    public async ValueTask<Unit> Handle(SetPrimaryPhoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var target = await db.Phones.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Teléfono no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        await PhonePrimaryWriter.MakePrimaryAsync(db, target, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
