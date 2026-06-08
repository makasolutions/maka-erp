using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Features;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.CreateParty;

public sealed class CreatePartyCommandHandler(PartiesDbContext db)
    : ICommandHandler<CreatePartyCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePartyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string typeCode = command.IdentificationTypeCode.Trim();
        string number = command.IdentificationNumber.Trim();

        bool exists = await db.Parties.AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.IdentificationTypeCode == typeCode && p.IdentificationNumber == number, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            throw new CustomException("Ya existe un tercero con esa identificación.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var roles = PartyMapping.NormalizeRoles(command.Roles);
        string legalName = PartyMapping.ResolveLegalName(command.Kind, command.LegalName, command.FirstName, command.LastName);
        int? dv = IdentificationValidator.ResolveVerificationDigit(typeCode, number, command.VerificationDigit);

        var party = Party.Create(
            typeCode, number, dv, command.Kind, legalName, roles,
            command.TradeName, command.Email, command.Website, command.TaxRegimeCode, command.FiscalResponsibilities,
            command.Status, command.Stage, command.LeadScore, command.SourceCode, command.AssignedUserId, command.MarketingType,
            command.BirthDate, command.GenderCode, command.MaritalStatusCode, command.CreditLimit, command.CreditCurrency,
            command.Notes, command.BranchId,
            command.FirstName, command.LastName, command.ActividadEconomicaCiiuCode,
            command.HasCredit, command.CreditDaysCode, command.CreditBlocked);

        party.ReplaceAddresses(PartyMapping.ToAddresses(command.Addresses));
        party.ReplaceContacts(PartyMapping.ToContacts(command.Contacts));
        party.ReplaceChannels(PartyMapping.ToChannels(command.Channels));
        party.ReplaceTeam(PartyMapping.ToTeam(command.Team));

        db.Parties.Add(party);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return party.Id;
    }
}
