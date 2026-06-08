using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.UpdateParty;

public static class UpdatePartyEndpoint
{
    public static RouteHandlerBuilder MapUpdatePartyEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdatePartyCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateParty")
            .WithSummary("Update third party")
            .RequirePermission(PartiesPermissions.Parties.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
}
