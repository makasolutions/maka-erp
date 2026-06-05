using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Bundles.RemoveBundleItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Bundles.RemoveBundleItem;

public static class RemoveBundleItemEndpoint
{
    public static RouteHandlerBuilder MapRemoveBundleItemEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{itemId:guid}",
                async (Guid productId, Guid itemId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new RemoveBundleItemCommand(productId, itemId), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("RemoveBundleItem")
            .WithSummary("Remove bundle item")
            .WithDescription("Removes an item from a Bundle product.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
