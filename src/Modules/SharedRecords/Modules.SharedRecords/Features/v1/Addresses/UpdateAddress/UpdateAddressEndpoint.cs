using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.UpdateAddress;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.UpdateAddress;

public static class UpdateAddressEndpoint
{
    public static RouteHandlerBuilder MapUpdateAddressEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateAddressCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("El id de la ruta no coincide con el cuerpo.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateAddress")
            .WithSummary("Update an address")
            .RequirePermission(SharedRecordsPermissions.Addresses.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
}
