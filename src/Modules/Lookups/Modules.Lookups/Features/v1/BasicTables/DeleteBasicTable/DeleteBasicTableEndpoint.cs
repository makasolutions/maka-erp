using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.DeleteBasicTable;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.DeleteBasicTable;

public static class DeleteBasicTableEndpoint
{
    public static RouteHandlerBuilder MapDeleteBasicTableEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteBasicTableCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteBasicTable")
            .WithSummary("Delete basic table")
            .RequirePermission(LookupsPermissions.Tables.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
}
