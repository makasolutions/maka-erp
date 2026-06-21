using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Parties.GetParties;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.GetParties;

public sealed class GetPartiesQueryHandler(PartiesDbContext db)
    : IQueryHandler<GetPartiesQuery, PagedResponse<PartyDto>>
{
    public async ValueTask<PagedResponse<PartyDto>> Handle(GetPartiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parties = db.Parties.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pat = $"%{query.Search.Trim()}%";
            parties = parties.Where(p =>
                EF.Functions.ILike(p.LegalName, pat) ||
                EF.Functions.ILike(p.IdentificationNumber, pat) ||
                (p.TradeName != null && EF.Functions.ILike(p.TradeName, pat)));
        }

        if (query.Kind.HasValue) parties = parties.Where(p => p.Kind == query.Kind.Value);

        // PR-F1a: el filtro por rol pasa a leer la faceta v2 activa (no la columna Roles v1).
        if (query.Role.HasValue && query.Role.Value != Contracts.Enums.PartyRole.None)
        {
            parties = query.Role.Value switch
            {
                Contracts.Enums.PartyRole.Customer => parties.Where(p => p.CustomerProfile != null && p.CustomerProfile.IsActive),
                Contracts.Enums.PartyRole.Supplier => parties.Where(p => p.SupplierProfile != null && p.SupplierProfile.IsActive),
                Contracts.Enums.PartyRole.Employee => parties.Where(p => p.EmployeeProfile != null && p.EmployeeProfile.IsActive),
                _ => parties,
            };
        }
        if (query.Status.HasValue) parties = parties.Where(p => p.Status == query.Status.Value);
        if (query.Stage.HasValue) parties = parties.Where(p => p.Stage == query.Stage.Value);
        if (query.AssignedUserId.HasValue) parties = parties.Where(p => p.AssignedUserId == query.AssignedUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            string cpat = $"%{query.City.Trim()}%";
            parties = parties.Where(p => p.Addresses.Any(a => a.City != null && EF.Functions.ILike(a.City, cpat)));
        }

        parties = (query.Sort?.ToLowerInvariant()) switch
        {
            "legalname" or "name" => parties.OrderBy(p => p.LegalName),
            "-legalname" or "-name" => parties.OrderByDescending(p => p.LegalName),
            "createdat" or "createdatutc" => parties.OrderBy(p => p.CreatedAtUtc),
            "-createdat" or "-createdatutc" => parties.OrderByDescending(p => p.CreatedAtUtc),
            _ => parties.OrderBy(p => p.LegalName),
        };

        return await parties
            .Select(p => new PartyDto(
                p.Id, p.IdentificationTypeCode, p.IdentificationNumber, p.VerificationDigit, p.Kind, p.LegalName,
                p.TradeName,
                // PR-F1a: Roles computado desde las facetas v2 activas (no la columna v1). Flags
                // disjuntos → suma == OR; EF lo traduce a CASE WHEN. Reconstrucción sin pérdida.
                (Contracts.Enums.PartyRole)(
                    (p.CustomerProfile != null && p.CustomerProfile.IsActive ? (int)Contracts.Enums.PartyRole.Customer : 0) +
                    (p.SupplierProfile != null && p.SupplierProfile.IsActive ? (int)Contracts.Enums.PartyRole.Supplier : 0) +
                    (p.EmployeeProfile != null && p.EmployeeProfile.IsActive ? (int)Contracts.Enums.PartyRole.Employee : 0)),
                p.Status, p.Stage, p.Email,
                p.Addresses.Where(a => a.IsPrimary).Select(a => a.City).FirstOrDefault()
                    ?? p.Addresses.Select(a => a.City).FirstOrDefault(),
                p.AssignedUserId, p.CreatedAtUtc,
                // PR-D5d: resumen v2 inline (cero joins nuevos): ejes fiscales (owned inline) + jerarquía.
                new PartyV2SummaryDto(
                    p.FiscalData == null ? null : p.FiscalData.RegimenTributario,
                    p.FiscalData == null ? null : p.FiscalData.ResponsabilidadIVA,
                    p.ParentPartyId)))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
