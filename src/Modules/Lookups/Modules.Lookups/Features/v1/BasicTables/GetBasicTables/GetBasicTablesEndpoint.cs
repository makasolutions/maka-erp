using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.BasicTables;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTables;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTables;

public static class GetBasicTablesEndpoint
{
    public static RouteHandlerBuilder MapGetBasicTablesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetBasicTablesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    PagedResponse<BasicTableDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetBasicTables")
            .WithSummary("List basic tables")
            .RequirePermission(LookupsPermissions.Tables.View)
            .Produces<PagedResponse<BasicTableDto>>(StatusCodes.Status200OK);
}
