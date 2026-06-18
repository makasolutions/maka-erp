using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.DeletePhone;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.DeletePhone;

public static class DeletePhoneEndpoint
{
    public static RouteHandlerBuilder MapDeletePhoneEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeletePhoneCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeletePhone")
            .WithSummary("Delete a phone (soft-delete)")
            .RequirePermission(SharedRecordsPermissions.Phones.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
