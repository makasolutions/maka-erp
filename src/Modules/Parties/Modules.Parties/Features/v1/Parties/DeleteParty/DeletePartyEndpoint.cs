using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Parties.DeleteParty;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Parties.Features.v1.Parties.DeleteParty;

public static class DeletePartyEndpoint
{
    public static RouteHandlerBuilder MapDeletePartyEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeletePartyCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteParty")
            .WithSummary("Soft-delete a third party")
            .RequirePermission(PartiesPermissions.Parties.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
