using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.RestoreParty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.RestoreParty;

public static class RestorePartyEndpoint
{
    public static RouteHandlerBuilder MapRestorePartyEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RestorePartyCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("RestoreParty")
            .WithSummary("Restore a soft-deleted third party")
            .RequirePermission(PartiesPermissions.Parties.Restore)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
