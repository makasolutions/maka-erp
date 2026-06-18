using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.CreateAddress;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.CreateAddress;

public static class CreateAddressEndpoint
{
    public static RouteHandlerBuilder MapCreateAddressEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateAddressCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/addresses/{id}", id);
                })
            .WithName("CreateAddress")
            .WithSummary("Create an address for an owner")
            .RequirePermission(SharedRecordsPermissions.Addresses.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
}
