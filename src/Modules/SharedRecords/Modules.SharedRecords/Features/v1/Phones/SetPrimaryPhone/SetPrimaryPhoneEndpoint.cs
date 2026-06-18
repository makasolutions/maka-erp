using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.SetPrimaryPhone;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.SetPrimaryPhone;

public static class SetPrimaryPhoneEndpoint
{
    public static RouteHandlerBuilder MapSetPrimaryPhoneEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/set-primary",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new SetPrimaryPhoneCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetPrimaryPhone")
            .WithSummary("Mark a phone as the owner's primary (demotes the others)")
            .RequirePermission(SharedRecordsPermissions.Phones.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
