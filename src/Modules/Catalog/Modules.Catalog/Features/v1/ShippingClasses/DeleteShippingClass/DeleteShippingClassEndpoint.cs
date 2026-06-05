using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.DeleteShippingClass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.DeleteShippingClass;

public static class DeleteShippingClassEndpoint
{
    public static RouteHandlerBuilder MapDeleteShippingClassEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new DeleteShippingClassCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteShippingClass")
            .WithSummary("Delete shipping class")
            .WithDescription("Deletes a shipping class. Fails if it is assigned to any product.")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
