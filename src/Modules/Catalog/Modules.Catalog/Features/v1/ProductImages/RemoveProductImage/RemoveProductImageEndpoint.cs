using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.RemoveProductImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.RemoveProductImage;

public static class RemoveProductImageEndpoint
{
    public static RouteHandlerBuilder MapRemoveProductImageEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{imageId:guid}",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new RemoveProductImageCommand(productId, imageId), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("RemoveProductImage")
            .WithSummary("Remove product image")
            .WithDescription("Removes an image from a product. If it was primary, the next image is promoted.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
