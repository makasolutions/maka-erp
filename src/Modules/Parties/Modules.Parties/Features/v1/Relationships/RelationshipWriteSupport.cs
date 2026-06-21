using System.Net;
using System.Text.Json;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.CustomFields;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Relationships;

/// <summary>
/// Lógica de dominio compartida por la creación de vínculos en AMBOS modos (Opción B, PR-3): el handler
/// atómico de <c>CreateParty</c> (lista en memoria) y el handler live de <c>CreatePartyRelationship</c>
/// (BD en tiempo real). Centralizar = una sola fuente de reglas (resolver-o-crear persona + validar
/// custom fields), sin divergencia entre modos.
/// </summary>
internal static class RelationshipWriteSupport
{
    /// <summary>Resuelve el <c>SourcePartyId</c> de un vínculo: si viene <paramref name="sourcePartyId"/>
    /// usa esa persona existente; si viene <paramref name="newPerson"/> primero deduplica por
    /// identificación (reusa si ya existe — el buscar-o-crear NO duplica) y, si no existe, crea una
    /// persona-Party mínima (Kind=Natural) y la AÑADE al contexto (se persiste en el SaveChanges del
    /// caller, misma transacción → sin huérfanos). No llama SaveChanges.</summary>
    public static async ValueTask<Guid> ResolveOrCreatePersonAsync(
        PartiesDbContext db, Guid? sourcePartyId, NewPersonInput? newPerson, CancellationToken ct)
    {
        if (sourcePartyId is { } id && id != Guid.Empty)
        {
            bool exists = await db.Parties.AsNoTracking()
                .AnyAsync(p => p.Id == id && !p.IsDeleted, ct).ConfigureAwait(false);
            if (!exists)
                throw new CustomException("La persona seleccionada no existe.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
            return id;
        }

        if (newPerson is null)
            throw new CustomException("Debe indicar la persona del contacto (existente o nueva).",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        string typeCode = newPerson.IdentificationTypeCode.Trim();
        string number = newPerson.IdentificationNumber.Trim();
        if (typeCode.Length == 0 || number.Length == 0)
            throw new CustomException("La persona nueva requiere tipo y número de identificación.",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        // Dedup por identificación dentro del tenant: si ya existe esa persona, se reusa (no se duplica).
        var existingId = await db.Parties.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IdentificationTypeCode == typeCode && p.IdentificationNumber == number)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (existingId is { } reuse) return reuse;

        string legalName = PartyMapping.ResolveLegalName(
            PartyKind.Natural, newPerson.LegalName ?? string.Empty, newPerson.FirstName, newPerson.LastName);
        if (string.IsNullOrWhiteSpace(legalName))
            throw new CustomException("La persona nueva requiere un nombre.",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        int? dv = IdentificationValidator.ResolveVerificationDigit(typeCode, number, newPerson.VerificationDigit);
        var person = Party.Create(
            typeCode, number, dv, PartyKind.Natural, legalName,
            firstName: newPerson.FirstName, lastName: newPerson.LastName);
        db.Parties.Add(person);
        return person.Id;
    }

    /// <summary>Valida los valores de custom fields (scope PartyRelationship) contra sus definiciones
    /// activas y devuelve el <see cref="JsonDocument"/> a persistir (o null si no hay valores). En alta
    /// usar <see cref="CustomFieldCompletenessMode.Minimal"/> (los requeridos NO bloquean — completitud
    /// gobernada). Lanza 400 con la lista de errores si algún valor es inválido.</summary>
    public static async ValueTask<JsonDocument?> BuildCustomFieldsAsync(
        PartiesDbContext db, IReadOnlyDictionary<string, JsonElement>? values,
        CustomFieldCompletenessMode mode, CancellationToken ct)
    {
        if (values is null || values.Count == 0) return null;

        var defs = await db.CustomFieldDefinitions.AsNoTracking()
            .Where(d => d.EntityType == CustomFieldEntityType.PartyRelationship && d.Activo)
            .ToListAsync(ct).ConfigureAwait(false);

        var errors = CustomFieldValues.ValidateAll(defs, values, mode);
        if (errors.Count > 0)
            throw new CustomException("Hay campos personalizados inválidos.", errors, HttpStatusCode.BadRequest);

        // Persistir solo los slugs conocidos (descarta claves huérfanas / definiciones inactivas).
        var slugs = defs.Select(d => d.ApiSlug).ToHashSet(StringComparer.Ordinal);
        var filtered = values.Where(kv => slugs.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        return filtered.Count == 0 ? null : JsonSerializer.SerializeToDocument(filtered);
    }
}
