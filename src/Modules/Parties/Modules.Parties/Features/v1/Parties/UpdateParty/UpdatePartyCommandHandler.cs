using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Features;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.UpdateParty;

public sealed class UpdatePartyCommandHandler(PartiesDbContext db)
    : ICommandHandler<UpdatePartyCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePartyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var party = await db.Parties
            .Include(p => p.Addresses).Include(p => p.Contacts).Include(p => p.Channels).Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var roles = PartyMapping.NormalizeRoles(command.Roles);
        string legalName = PartyMapping.ResolveLegalName(command.Kind, command.LegalName, command.FirstName, command.LastName);

        party.Update(command.Kind, legalName, roles, command.TradeName, command.Email, command.Website,
            command.TaxRegimeCode, command.FiscalResponsibilities, command.Status, command.Stage, command.LeadScore,
            command.SourceCode, command.AssignedUserId, command.MarketingType, command.BirthDate, command.GenderCode,
            command.MaritalStatusCode, command.CreditLimit, command.CreditCurrency, command.Notes, command.BranchId,
            command.VerificationDigit,
            command.FirstName, command.LastName, command.ActividadEconomicaCiiuCode,
            command.HasCredit, command.CreditDaysCode, command.CreditBlocked);

        party.ReplaceAddresses(PartyMapping.ToAddresses(command.Addresses));
        party.ReplaceContacts(PartyMapping.ToContacts(command.Contacts));
        party.ReplaceChannels(PartyMapping.ToChannels(command.Channels));
        party.ReplaceTeam(PartyMapping.ToTeam(command.Team));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return party.Id;
    }
}
