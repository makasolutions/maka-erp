using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTableById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTableById;

public static class GetBasicTableByIdEndpoint
{
    public static RouteHandlerBuilder MapGetBasicTableByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    BasicTableDetailDto result = await mediator.Send(new GetBasicTableByIdQuery(id), cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetBasicTableById")
            .WithSummary("Get basic table with its records")
            .RequirePermission(LookupsPermissions.Tables.View)
            .Produces<BasicTableDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
}
