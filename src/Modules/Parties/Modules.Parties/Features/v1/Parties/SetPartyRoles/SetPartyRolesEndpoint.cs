using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.SetPartyRoles;

public static class SetPartyRolesEndpoint
{
    public static RouteHandlerBuilder MapSetPartyRolesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/roles",
                async (Guid id, SetPartyRolesCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetPartyRoles")
            .WithSummary("Set the roles (customer/supplier) of a third party")
            .RequirePermission(PartiesPermissions.Parties.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
