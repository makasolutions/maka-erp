using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.UpdateBasicTable;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.UpdateBasicTable;

public static class UpdateBasicTableEndpoint
{
    public static RouteHandlerBuilder MapUpdateBasicTableEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateBasicTableCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateBasicTable")
            .WithSummary("Update basic table")
            .RequirePermission(LookupsPermissions.Tables.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
}
