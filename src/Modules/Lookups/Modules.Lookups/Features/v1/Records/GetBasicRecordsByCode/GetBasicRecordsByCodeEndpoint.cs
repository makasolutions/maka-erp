using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.Records.GetBasicRecordsByCode;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.Records.GetBasicRecordsByCode;

public static class GetBasicRecordsByCodeEndpoint
{
    public static RouteHandlerBuilder MapGetBasicRecordsByCodeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/by-code/{tableCode}",
                async (string tableCode, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<BasicRecordBriefDto> result =
                        await mediator.Send(new GetBasicRecordsByCodeQuery(tableCode), cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetBasicRecordsByCode")
            .WithSummary("Active records of a basic table by its code (for dropdowns)")
            .RequirePermission(LookupsPermissions.Tables.View)
            .Produces<IReadOnlyList<BasicRecordBriefDto>>(StatusCodes.Status200OK);
}
