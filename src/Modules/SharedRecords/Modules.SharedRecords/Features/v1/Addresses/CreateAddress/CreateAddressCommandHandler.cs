using FSH.Modules.SharedRecords.Contracts.v1.Addresses.CreateAddress;
using FSH.Modules.SharedRecords.Data;
using FSH.Modules.SharedRecords.Domain;
using Mediator;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.CreateAddress;

public sealed class CreateAddressCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<CreateAddressCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAddressCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = Address.Create(
            command.OwnerType, command.OwnerId, command.LabelCode, command.IsActive, command.IsPrimary,
            command.Country, command.Department, command.City, command.DepartmentCode, command.MunicipalityCode,
            command.Line, command.Barrio, command.Reference, command.Latitude, command.Longitude);

        db.Addresses.Add(address);

        if (address.IsPrimary)
        {
            // demote→promote (índice único parcial no diferible) — ver AddressPrimaryWriter.
            await AddressPrimaryWriter.MakePrimaryAsync(db, address, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return address.Id;
    }
}
