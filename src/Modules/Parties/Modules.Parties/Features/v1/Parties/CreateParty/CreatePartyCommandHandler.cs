using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Features;
using FSH.Modules.Parties.Features.Sync;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.CreateParty;

public sealed class CreatePartyCommandHandler(PartiesDbContext db, PartyV2Synchronizer synchronizer)
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
            typeCode, number, dv, command.Kind, legalName,
            command.TradeName, command.Email, command.Website,
            command.Status, command.Stage, command.LeadScore, command.SourceCode, command.AssignedUserId, command.MarketingType,
            command.BirthDate, command.GenderCode, command.MaritalStatusCode,
            command.Notes, command.BranchId,
            command.FirstName, command.LastName);

        party.ReplaceAddresses(PartyMapping.ToAddresses(command.Addresses));
        party.ReplaceContacts(PartyMapping.ToContacts(command.Contacts));
        party.ReplaceChannels(PartyMapping.ToChannels(command.Channels));
        party.ReplaceTeam(PartyMapping.ToTeam(command.Team));

        db.Parties.Add(party);

        // Escritura v2 (PR-D5 → PR-F1a escritura primaria): mismo DbContext → mismo SaveChanges →
        // misma transacción. El synchronizer lee del input en lenguaje v1 (construido desde el
        // comando), NO de las propiedades v1 del party. Tercero nuevo: navs v2 null → crea profiles
        // (D5a) y crédito (D5b). Party.Create además escribió las columnas v1 (redundante hasta F1b).
        var v2Input = new PartyV2WriteInput(roles, command.CreditLimit, command.CreditCurrency,
            command.CreditDaysCode, command.CreditBlocked, command.TaxRegimeCode, command.ActividadEconomicaCiiuCode,
            command.FiscalAxes);
        await synchronizer.SyncAsync(party, v2Input, cancellationToken).ConfigureAwait(false);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (
            (ex.InnerException?.Message.Contains("23505", StringComparison.Ordinal) ?? false) &&
            (ex.InnerException?.Message.Contains("IdentificationNumber", StringComparison.Ordinal) ?? false))
        {
            // Carrera contra el índice único (TenantId, tipo, número): degradar a 409 limpio.
            throw new CustomException("Ya existe un tercero con esa identificación.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }
        return party.Id;
    }
}
