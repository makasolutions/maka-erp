using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.GetPartyById;

public static class GetPartyByIdEndpoint
{
    public static RouteHandlerBuilder MapGetPartyByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    PartyDetailDto result = await mediator.Send(new GetPartyByIdQuery(id), cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetPartyById")
            .WithSummary("Get a third party with children")
            .RequirePermission(PartiesPermissions.Parties.View)
            .Produces<PartyDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
}
