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

namespace FSH.Modules.Parties.Features.v1.Relationships.GetPartyRelationshipsBySource;

public sealed class GetPartyRelationshipsBySourceQueryValidator : AbstractValidator<GetPartyRelationshipsBySourceQuery>
{
    public GetPartyRelationshipsBySourceQueryValidator() => RuleFor(x => x.SourcePartyId).NotEmpty();
}

public sealed class GetPartyRelationshipsBySourceQueryHandler(PartiesDbContext db)
    : IQueryHandler<GetPartyRelationshipsBySourceQuery, IReadOnlyList<PartyRelationshipBySourceDto>>
{
    public async ValueTask<IReadOnlyList<PartyRelationshipBySourceDto>> Handle(
        GetPartyRelationshipsBySourceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.PartyRelationships.AsNoTracking().Where(r => r.SourcePartyId == query.SourcePartyId);
        if (!query.IncludeInactive) q = q.Where(r => r.IsActive);

        // Dirección B (solo lectura): a qué empresas está vinculada la persona. Nombre de la empresa
        // (Target) resuelto en el backend. Principal primero.
        return await q
            .OrderByDescending(r => r.IsPrimary).ThenBy(r => r.StartDate)
            .Select(r => new PartyRelationshipBySourceDto(
                r.Id, r.TargetPartyId,
                db.Parties.Where(p => p.Id == r.TargetPartyId).Select(p => p.LegalName).FirstOrDefault(),
                r.RelationshipTypeCode, r.ContactFunctionCode, r.JobTitleCode,
                r.IsPrimary, r.IsActive, r.StartDate, r.EndDate))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public static class GetPartyRelationshipsBySourceEndpoint
{
    public static RouteHandlerBuilder MapGetPartyRelationshipsBySourceEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/relationships/by-source",
                async ([AsParameters] GetPartyRelationshipsBySourceQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<PartyRelationshipBySourceDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetPartyRelationshipsBySource")
            .WithSummary("List companies a person is linked to (read-only, direction B)")
            .RequirePermission(PartiesPermissions.Relationships.View)
            .Produces<IReadOnlyList<PartyRelationshipBySourceDto>>(StatusCodes.Status200OK);
}
