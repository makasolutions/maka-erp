using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.CreateParty;

public static class CreatePartyEndpoint
{
    public static RouteHandlerBuilder MapCreatePartyEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreatePartyCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/parties/{id}", id);
                })
            .WithName("CreateParty")
            .WithSummary("Create third party (tercero)")
            .RequirePermission(PartiesPermissions.Parties.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
}
