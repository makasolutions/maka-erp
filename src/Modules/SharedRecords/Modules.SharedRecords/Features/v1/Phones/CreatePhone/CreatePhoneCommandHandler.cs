using FSH.Modules.SharedRecords.Contracts.v1.Phones.CreatePhone;
using FSH.Modules.SharedRecords.Data;
using FSH.Modules.SharedRecords.Domain;
using Mediator;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.CreatePhone;

public sealed class CreatePhoneCommandHandler(SharedRecordsDbContext db)
    : ICommandHandler<CreatePhoneCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePhoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var phone = Phone.Create(
            command.OwnerType, command.OwnerId, command.TypeCode, command.IsActive, command.IsPrimary,
            command.Number, command.Extension, command.CountryCode);

        db.Phones.Add(phone);

        if (phone.IsPrimary)
        {
            // demote→promote (índice único parcial no diferible) — ver PhonePrimaryWriter.
            await PhonePrimaryWriter.MakePrimaryAsync(db, phone, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return phone.Id;
    }
}
