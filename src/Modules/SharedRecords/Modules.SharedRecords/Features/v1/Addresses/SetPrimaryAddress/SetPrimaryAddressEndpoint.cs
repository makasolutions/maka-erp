using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.SetPrimaryAddress;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.SetPrimaryAddress;

public static class SetPrimaryAddressEndpoint
{
    public static RouteHandlerBuilder MapSetPrimaryAddressEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/set-primary",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new SetPrimaryAddressCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetPrimaryAddress")
            .WithSummary("Mark an address as the owner's primary (demotes the others)")
            .RequirePermission(SharedRecordsPermissions.Addresses.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
