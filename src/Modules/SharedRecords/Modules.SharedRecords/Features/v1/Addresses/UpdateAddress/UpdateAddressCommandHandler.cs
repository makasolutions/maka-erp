using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.UpdateAddress;
using FSH.Modules.SharedRecords.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.UpdateAddress;

public sealed class UpdateAddressCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<UpdateAddressCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateAddressCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = await db.Addresses.FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Dirección no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // Aplica los campos; la promoción a principal la maneja el writer (demote→promote) para no
        // violar el índice único parcial. Se setea IsPrimary=false aquí y el writer promueve si aplica.
        address.Update(
            command.LabelCode, command.IsActive, isPrimary: false,
            command.Country, command.Department, command.City, command.DepartmentCode, command.MunicipalityCode,
            command.Line, command.Barrio, command.Reference, command.Latitude, command.Longitude);

        if (command.IsPrimary)
        {
            await AddressPrimaryWriter.MakePrimaryAsync(db, address, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return address.Id;
    }
}
