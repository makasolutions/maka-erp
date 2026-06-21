using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.CustomFields;
using FSH.Modules.Parties.Domain.Relationships;
using FSH.Modules.Parties.Features;
using FSH.Modules.Parties.Features.v1.Relationships;
using FSH.Modules.Parties.Sync;
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

        // PR-3 (Opción B): contactos acumulados en el wizard → se persisten en la MISMA transacción que
        // la empresa (un solo SaveChanges). Cero huérfanos: la persona nueva se crea acá, no antes.
        await AddRelationshipsAsync(party.Id, command, cancellationToken).ConfigureAwait(false);

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

    /// <summary>Crea los <see cref="PartyRelationship"/> de la creación atómica (Opción B). Valida el
    /// invariante "1 principal por empresa" entre las líneas (espejo de la lógica en memoria del front)
    /// y deduplica personas nuevas dentro del mismo lote por identificación. No llama SaveChanges: se
    /// persiste con la empresa.</summary>
    private async Task AddRelationshipsAsync(Guid companyId, CreatePartyCommand command, CancellationToken ct)
    {
        if (command.Relationships is not { Count: > 0 } lines) return;

        if (lines.Count(l => l.IsPrimary) > 1)
            throw new CustomException("Solo un contacto puede ser el principal de la empresa.",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        // Dedup de personas NUEVAS dentro del lote (aún no persistidas, no las ve la query AsNoTracking).
        var newPersonInBatch = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            Guid sourceId;
            if (line.SourcePartyId is { } sid && sid != Guid.Empty)
            {
                sourceId = await RelationshipWriteSupport
                    .ResolveOrCreatePersonAsync(db, sid, null, ct).ConfigureAwait(false);
            }
            else if (line.NewPerson is { } np)
            {
                string key = $"{np.IdentificationTypeCode.Trim()}|{np.IdentificationNumber.Trim()}";
                if (!newPersonInBatch.TryGetValue(key, out sourceId))
                {
                    sourceId = await RelationshipWriteSupport
                        .ResolveOrCreatePersonAsync(db, null, np, ct).ConfigureAwait(false);
                    newPersonInBatch[key] = sourceId;
                }
            }
            else
            {
                throw new CustomException("Cada contacto requiere una persona (existente o nueva).",
                    Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
            }

            if (sourceId == companyId)
                throw new CustomException("Una persona no puede vincularse a sí misma.",
                    Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

            var customFields = await RelationshipWriteSupport
                .BuildCustomFieldsAsync(db, line.CustomFields, CustomFieldCompletenessMode.Minimal, ct)
                .ConfigureAwait(false);

            var rel = PartyRelationship.Create(
                sourceId, companyId, line.RelationshipTypeCode,
                line.ContactFunctionCode, line.JobTitleCode, line.IsPrimary,
                line.StartDate, line.EndDate);
            rel.SetCustomFields(customFields);
            db.PartyRelationships.Add(rel);
        }
    }
}
