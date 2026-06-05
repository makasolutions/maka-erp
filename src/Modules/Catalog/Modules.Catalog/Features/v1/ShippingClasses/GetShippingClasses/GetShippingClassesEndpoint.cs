using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.GetShippingClasses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.GetShippingClasses;

public static class GetShippingClassesEndpoint
{
    public static RouteHandlerBuilder MapGetShippingClassesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetShippingClassesQuery(), cancellationToken)))
            .WithName("GetShippingClasses")
            .WithSummary("List shipping classes")
            .WithDescription("Returns the configured shipping classes (spec §2.4).")
            .RequirePermission(CatalogPermissions.Settings.View)
            .Produces<IReadOnlyList<ShippingClassDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
