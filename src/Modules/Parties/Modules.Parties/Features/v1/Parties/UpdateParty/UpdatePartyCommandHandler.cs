using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Features;
using FSH.Modules.Parties.Features.Sync;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.UpdateParty;

public sealed class UpdatePartyCommandHandler(PartiesDbContext db, PartyV2Synchronizer synchronizer)
    : ICommandHandler<UpdatePartyCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePartyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var party = await db.Parties
            .Include(p => p.Addresses).Include(p => p.Contacts).Include(p => p.Channels).Include(p => p.Team)
            // Navs v2 cargadas para que el synchronizer diffee el estado actual (D5a profiles, D5c CIIU).
            .Include(p => p.CustomerProfile).Include(p => p.SupplierProfile).Include(p => p.EmployeeProfile)
            .Include(p => p.CiiuActivities)
            .FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var roles = PartyMapping.NormalizeRoles(command.Roles);
        string legalName = PartyMapping.ResolveLegalName(command.Kind, command.LegalName, command.FirstName, command.LastName);
        int? dv = IdentificationValidator.ResolveVerificationDigit(
            party.IdentificationTypeCode, party.IdentificationNumber, command.VerificationDigit);

        party.Update(command.Kind, legalName, command.TradeName, command.Email, command.Website,
            command.Status, command.Stage, command.LeadScore,
            command.SourceCode, command.AssignedUserId, command.MarketingType, command.BirthDate, command.GenderCode,
            command.MaritalStatusCode, command.Notes, command.BranchId,
            dv,
            command.FirstName, command.LastName);

        party.ReplaceAddresses(PartyMapping.ToAddresses(command.Addresses));
        party.ReplaceContacts(PartyMapping.ToContacts(command.Contacts));
        party.ReplaceChannels(PartyMapping.ToChannels(command.Channels));
        party.ReplaceTeam(PartyMapping.ToTeam(command.Team));

        // Escritura v2 (PR-D5 → PR-F1a escritura primaria): mismo DbContext → mismo SaveChanges.
        // El synchronizer lee del input en lenguaje v1 (desde el comando), NO de las propiedades v1.
        // Corre ANTES del DetectChanges de abajo: lo NUEVO (profiles, CreditAccount, movimientos,
        // holds) se agrega vía db.*.Add (estado Added explícito, sobrevive AutoDetectChangesEnabled=
        // false); las mutaciones sobre entidades ya rastreadas (profiles incluidos, CreditAccount
        // cargado para diffear) las marca Modified el DetectChanges siguiente. Nada de esto está en
        // las colecciones de MarkChildrenAdded (solo hijos v1), así que esa danza no los afecta.
        var v2Input = new PartyV2WriteInput(roles, command.CreditLimit, command.CreditCurrency,
            command.CreditDaysCode, command.CreditBlocked, command.TaxRegimeCode, command.ActividadEconomicaCiiuCode,
            command.FiscalAxes);
        await synchronizer.SyncAsync(party, v2Input, cancellationToken).ConfigureAwait(false);

        // En un grafo ya rastreado, EF trata los hijos NUEVOS (con GUID generado en
        // cliente) como filas existentes → genera UPDATE (PartyId 0→real) que afecta
        // 0 filas (DbUpdateConcurrencyException). Tras Clear()+Add las colecciones
        // solo contienen los nuevos: forzarlos a Added y desactivar AutoDetectChanges
        // durante el SaveChanges para que el DetectChanges final no revierta el estado.
        db.ChangeTracker.DetectChanges();
        MarkChildrenAdded(party.Addresses);
        MarkChildrenAdded(party.Contacts);
        MarkChildrenAdded(party.Channels);
        MarkChildrenAdded(party.Team);

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        return party.Id;
    }

    private void MarkChildrenAdded<T>(IEnumerable<T> children) where T : class
    {
        foreach (var child in children)
        {
            var entry = db.Entry(child);
            if (entry.State is EntityState.Modified or EntityState.Unchanged)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
