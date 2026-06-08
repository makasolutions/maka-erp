using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Parties.GetParties;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.GetParties;

public static class GetPartiesEndpoint
{
    public static RouteHandlerBuilder MapGetPartiesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetPartiesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    PagedResponse<PartyDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetParties")
            .WithSummary("List third parties")
            .RequirePermission(PartiesPermissions.Parties.View)
            .Produces<PagedResponse<PartyDto>>(StatusCodes.Status200OK);
}
