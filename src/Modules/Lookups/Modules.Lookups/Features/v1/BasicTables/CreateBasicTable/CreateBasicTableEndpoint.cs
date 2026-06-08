using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.CreateBasicTable;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.CreateBasicTable;

public static class CreateBasicTableEndpoint
{
    public static RouteHandlerBuilder MapCreateBasicTableEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateBasicTableCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/lookups/tables/{id}", id);
                })
            .WithName("CreateBasicTable")
            .WithSummary("Create basic table")
            .WithDescription("Creates a basic (lookup) table for the current tenant, or global when root.")
            .RequirePermission(LookupsPermissions.Tables.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);
}
