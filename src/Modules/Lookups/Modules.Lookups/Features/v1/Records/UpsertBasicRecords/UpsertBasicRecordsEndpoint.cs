using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.Records.UpsertBasicRecords;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.Records.UpsertBasicRecords;

public static class UpsertBasicRecordsEndpoint
{
    public static RouteHandlerBuilder MapUpsertBasicRecordsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/records",
                async (Guid id, UpsertBasicRecordsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpsertBasicRecords")
            .WithSummary("Replace/update the records of a basic table (batch)")
            .RequirePermission(LookupsPermissions.Tables.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
}
