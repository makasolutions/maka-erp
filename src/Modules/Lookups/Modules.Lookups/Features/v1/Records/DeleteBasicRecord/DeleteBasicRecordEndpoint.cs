using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Lookups.Contracts.Authorization;
using FSH.Modules.Lookups.Contracts.v1.Records.DeleteBasicRecord;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Lookups.Features.v1.Records.DeleteBasicRecord;

public static class DeleteBasicRecordEndpoint
{
    public static RouteHandlerBuilder MapDeleteBasicRecordEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}/records/{recordId:guid}",
                async (Guid id, Guid recordId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteBasicRecordCommand(id, recordId), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteBasicRecord")
            .WithSummary("Delete a record from a basic table")
            .RequirePermission(LookupsPermissions.Tables.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
}
