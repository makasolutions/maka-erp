using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.DeletePhone;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.DeletePhone;

public sealed class DeletePhoneCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<DeletePhoneCommand>
{
    public async ValueTask<Unit> Handle(DeletePhoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var phone = await db.Phones.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Teléfono no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        db.Phones.Remove(phone); // soft-delete vía el interceptor de auditoría
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
