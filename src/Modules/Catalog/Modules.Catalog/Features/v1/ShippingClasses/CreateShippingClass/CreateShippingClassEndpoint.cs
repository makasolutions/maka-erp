using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.CreateShippingClass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.CreateShippingClass;

public static class CreateShippingClassEndpoint
{
    public static RouteHandlerBuilder MapCreateShippingClassEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateShippingClassCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/shipping-classes/{id}", id);
                })
            .WithName("CreateShippingClass")
            .WithSummary("Create shipping class")
            .WithDescription("Creates a shipping class (spec §2.4).")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
