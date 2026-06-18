using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.CreatePhone;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.CreatePhone;

public static class CreatePhoneEndpoint
{
    public static RouteHandlerBuilder MapCreatePhoneEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreatePhoneCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/phones/{id}", id);
                })
            .WithName("CreatePhone")
            .WithSummary("Create a phone for an owner")
            .RequirePermission(SharedRecordsPermissions.Phones.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
}
