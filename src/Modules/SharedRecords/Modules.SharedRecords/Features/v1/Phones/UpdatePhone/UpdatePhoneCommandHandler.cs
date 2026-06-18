using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.UpdatePhone;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.UpdatePhone;

public sealed class UpdatePhoneCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<UpdatePhoneCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePhoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var phone = await db.Phones.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Teléfono no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // Aplica los campos; la promoción a principal la maneja el writer (demote→promote) para no
        // violar el índice único parcial. Se setea IsPrimary=false aquí y el writer promueve si aplica.
        phone.Update(
            command.TypeCode, command.IsActive, isPrimary: false,
            command.Number, command.Extension, command.CountryCode);

        if (command.IsPrimary)
        {
            await PhonePrimaryWriter.MakePrimaryAsync(db, phone, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return phone.Id;
    }
}
