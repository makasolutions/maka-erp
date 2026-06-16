using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.GetPartyById;

public sealed class GetPartyByIdQueryHandler(PartiesDbContext db)
    : IQueryHandler<GetPartyByIdQuery, PartyDetailDto>
{
    public async ValueTask<PartyDetailDto> Handle(GetPartyByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Proyección principal: campos v1 (sin cambio, frontera Catalog) + V2 alcanzable por nav
        // (profiles, FiscalData/LegalRep owned inline, CIIU[], ParentPartyId). Crédito y holds NO son
        // navs de Party → se traen en 2 follow-up queries (barato: es UN registro de detalle).
        var p = await db.Parties.AsNoTracking()
            .Where(x => x.Id == query.Id && !x.IsDeleted)
            .Select(x => new PartyDetailDto(
                x.Id, x.IdentificationTypeCode, x.IdentificationNumber, x.VerificationDigit, x.Kind, x.LegalName,
                x.FirstName, x.LastName, x.TradeName, x.Email, x.Website, x.TaxRegimeCode, x.FiscalResponsibilities,
                x.ActividadEconomicaCiiuCode, x.Roles, x.Status, x.Stage,
                x.LeadScore, x.SourceCode, x.AssignedUserId, x.MarketingType, x.BirthDate, x.GenderCode, x.MaritalStatusCode,
                x.HasCredit, x.CreditLimit, x.CreditDaysCode, x.CreditBlocked, x.CreditCurrency, x.Notes, x.BranchId,
                x.IsGlobalSupplier, x.CreatedAtUtc,
                x.Addresses.Select(a => new PartyAddressDto(a.Id, a.Country, a.Department, a.City, a.Line, a.Barrio, a.Reference,
                    a.Latitude, a.Longitude, a.IsPrimary, a.LabelCode, a.DepartmentCode, a.MunicipalityCode, a.NormalizedLine)).ToList(),
                x.Contacts.Select(c => new PartyContactDto(c.Id, c.Reference, c.ContactTypeCode, c.AreaCode,
                    c.IdentificationTypeCode, c.IdentificationNumber, c.FirstName, c.LastName, c.PositionCode,
                    c.ProfessionCode, c.BirthDate, c.GenderCode, c.MaritalStatusCode, c.Email, c.Phone, c.Cell,
                    c.IsCommercial, c.Notes)).ToList(),
                x.Channels.Select(c => new PartyChannelDto(c.Id, c.ChannelTypeCode, c.Value, c.Reference, c.IsPrimary)).ToList(),
                x.Team.Select(m => new PartyTeamMemberDto(m.Id, m.UserId, m.Role)).ToList(),
                new PartyV2DetailDto(
                    x.ParentPartyId,
                    x.FiscalData == null ? null : new PartyV2FiscalDto(
                        x.FiscalData.RegimenTributario, x.FiscalData.ResponsabilidadIVA,
                        x.FiscalData.GranContribuyente, x.FiscalData.Autorretenedor,
                        x.FiscalData.AgenteRetencionIVA, x.FiscalData.AgenteRetencionICA,
                        x.FiscalData.ObligadoLlevarContabilidad, x.FiscalData.FlagPEP,
                        x.FiscalData.FormaJuridica, x.FiscalData.ResponsabilidadesFiscales),
                    x.LegalRepresentative == null ? null : new PartyV2LegalRepDto(
                        x.LegalRepresentative.Nombres, x.LegalRepresentative.Apellidos,
                        x.LegalRepresentative.TipoIdentificacion, x.LegalRepresentative.NumeroIdentificacion,
                        x.LegalRepresentative.Telefono, x.LegalRepresentative.Celular,
                        x.LegalRepresentative.Email, x.LegalRepresentative.EsPEP),
                    x.CustomerProfile == null ? null : new PartyV2CustomerProfileDto(
                        x.CustomerProfile.ClassificationId, x.CustomerProfile.PriceListId,
                        x.CustomerProfile.DefaultSalespersonId, x.CustomerProfile.MaxDiscountPct,
                        x.CustomerProfile.AllowDiscount, x.CustomerProfile.IsActive),
                    x.SupplierProfile == null ? null : new PartyV2SupplierProfileDto(
                        x.SupplierProfile.ClassificationId, x.SupplierProfile.DefaultCurrencyId,
                        x.SupplierProfile.PaymentTerms.DiasCredito, x.SupplierProfile.IsDropshipping,
                        x.SupplierProfile.LeadTimeDays, x.SupplierProfile.IsActive),
                    x.ContactProfile == null ? null : new PartyV2ContactProfileDto(
                        x.ContactProfile.JobTitle, x.ContactProfile.ContactFunction,
                        x.ContactProfile.IsCommercialContact, x.ContactProfile.IsPrimary),
                    x.PartnerProfile == null ? null : new PartyV2PartnerProfileDto(
                        x.PartnerProfile.SharePercentage, x.PartnerProfile.StartDate,
                        x.PartnerProfile.EndDate, x.PartnerProfile.Status),
                    x.EmployeeProfile == null ? null : new PartyV2EmployeeProfileDto(
                        x.EmployeeProfile.EmployeeCode, x.EmployeeProfile.JobTitle,
                        x.EmployeeProfile.IsSalesperson, x.EmployeeProfile.IsCollector, x.EmployeeProfile.IsActive),
                    x.CiiuActivities.Select(c => new PartyV2CiiuDto(c.CiiuCode, c.IsPrincipal)).ToList(),
                    null,                              // Credit → follow-up
                    new List<PartyV2HoldDto>())))      // ActiveHolds → follow-up
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (p is null)
            throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // Follow-up 1: crédito (CreditAccount vía join a CustomerProfile del party).
        var credit = await (
            from a in db.CreditAccounts.AsNoTracking()
            join cp in db.CustomerProfiles.AsNoTracking() on a.CustomerProfileId equals cp.Id
            where cp.PartyId == query.Id
            select new PartyV2CreditDto(a.CupoAsignado, a.SaldoDisponible, a.EstaActivo, a.MonedaId, a.DiasCredito))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Follow-up 2: holds activos por PartyId (PartyHold no es nav de Party).
        var holds = await db.PartyHolds.AsNoTracking()
            .Where(h => h.PartyId == query.Id && h.EstaActivo)
            .Select(h => new PartyV2HoldDto(h.HoldType, h.Motivo, h.FechaInicio, h.FechaLiberacion))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return p with { V2 = p.V2! with { Credit = credit, ActiveHolds = holds } };
    }
}
