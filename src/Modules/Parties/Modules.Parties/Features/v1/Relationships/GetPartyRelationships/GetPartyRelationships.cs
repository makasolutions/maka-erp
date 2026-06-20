using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Relationships.GetPartyRelationships;

public sealed class GetPartyRelationshipsQueryValidator : AbstractValidator<GetPartyRelationshipsQuery>
{
    public GetPartyRelationshipsQueryValidator() => RuleFor(x => x.TargetPartyId).NotEmpty();
}

public sealed class GetPartyRelationshipsQueryHandler(PartiesDbContext db)
    : IQueryHandler<GetPartyRelationshipsQuery, IReadOnlyList<PartyRelationshipDto>>
{
    public async ValueTask<IReadOnlyList<PartyRelationshipDto>> Handle(
        GetPartyRelationshipsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.PartyRelationships.AsNoTracking().Where(r => r.TargetPartyId == query.TargetPartyId);
        if (!query.IncludeInactive) q = q.Where(r => r.IsActive);

        // Nombre de la persona resuelto en el backend (evita N+1 en el front). Principal primero.
        return await q
            .OrderByDescending(r => r.IsPrimary).ThenBy(r => r.StartDate)
            .Select(r => new PartyRelationshipDto(
                r.Id, r.SourcePartyId,
                db.Parties.Where(p => p.Id == r.SourcePartyId).Select(p => p.LegalName).FirstOrDefault(),
                r.TargetPartyId, r.RelationshipTypeCode, r.ContactFunctionCode, r.JobTitleCode,
                r.IsPrimary, r.IsActive, r.StartDate, r.EndDate))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public static class GetPartyRelationshipsEndpoint
{
    public static RouteHandlerBuilder MapGetPartyRelationshipsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/relationships",
                async ([AsParameters] GetPartyRelationshipsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<PartyRelationshipDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetPartyRelationships")
            .WithSummary("List relationships (contacts) of a company")
            .RequirePermission(PartiesPermissions.Relationships.View)
            .Produces<IReadOnlyList<PartyRelationshipDto>>(StatusCodes.Status200OK);
}
