using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.GetAddresses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.GetAddresses;

public static class GetAddressesEndpoint
{
    public static RouteHandlerBuilder MapGetAddressesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (string ownerType, Guid ownerId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<AddressDto> items =
                        await mediator.Send(new GetAddressesQuery(ownerType, ownerId), cancellationToken);
                    return TypedResults.Ok(items);
                })
            .WithName("GetAddresses")
            .WithSummary("List addresses scoped to an owner (ownerType + ownerId)")
            .RequirePermission(SharedRecordsPermissions.Addresses.View)
            .Produces<IReadOnlyList<AddressDto>>(StatusCodes.Status200OK);
}
