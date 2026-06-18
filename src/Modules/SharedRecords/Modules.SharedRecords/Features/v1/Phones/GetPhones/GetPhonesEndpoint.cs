using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.SharedRecords.Contracts.Authorization;
using FSH.Modules.SharedRecords.Contracts.v1.Phones;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.GetPhones;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.GetPhones;

public static class GetPhonesEndpoint
{
    public static RouteHandlerBuilder MapGetPhonesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (string ownerType, Guid ownerId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<PhoneDto> items =
                        await mediator.Send(new GetPhonesQuery(ownerType, ownerId), cancellationToken);
                    return TypedResults.Ok(items);
                })
            .WithName("GetPhones")
            .WithSummary("List phones scoped to an owner (ownerType + ownerId)")
            .RequirePermission(SharedRecordsPermissions.Phones.View)
            .Produces<IReadOnlyList<PhoneDto>>(StatusCodes.Status200OK);
}
