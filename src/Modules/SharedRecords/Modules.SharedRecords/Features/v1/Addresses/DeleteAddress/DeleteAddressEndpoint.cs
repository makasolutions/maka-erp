using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.DeleteAddress;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.DeleteAddress;

public static class DeleteAddressEndpoint
{
    public static RouteHandlerBuilder MapDeleteAddressEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteAddressCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteAddress")
            .WithSummary("Delete an address (soft-delete)")
            .RequirePermission(SharedRecordsPermissions.Addresses.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
