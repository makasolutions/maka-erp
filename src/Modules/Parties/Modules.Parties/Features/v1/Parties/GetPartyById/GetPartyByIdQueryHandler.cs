using System.Net;
using FSH.Framework.Core.Exceptions;
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
                    a.Latitude, a.Longitude, a.IsPrimary, a.LabelCode)).ToList(),
                x.Contacts.Select(c => new PartyContactDto(c.Id, c.Reference, c.ContactTypeCode, c.AreaCode,
                    c.IdentificationTypeCode, c.IdentificationNumber, c.FirstName, c.LastName, c.PositionCode,
                    c.ProfessionCode, c.BirthDate, c.GenderCode, c.MaritalStatusCode, c.Email, c.Phone, c.Cell,
                    c.IsCommercial, c.Notes)).ToList(),
                x.Channels.Select(c => new PartyChannelDto(c.Id, c.ChannelTypeCode, c.Value, c.Reference, c.IsPrimary)).ToList(),
                x.Team.Select(m => new PartyTeamMemberDto(m.Id, m.UserId, m.Role)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return p ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
    }
}
