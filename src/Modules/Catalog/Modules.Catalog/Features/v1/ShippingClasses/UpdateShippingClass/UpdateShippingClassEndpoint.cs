using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.UpdateShippingClass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.UpdateShippingClass;

public static class UpdateShippingClassEndpoint
{
    public static RouteHandlerBuilder MapUpdateShippingClassEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateShippingClassCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { Id = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateShippingClass")
            .WithSummary("Update shipping class")
            .WithDescription("Updates a shipping class (spec §2.4).")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
