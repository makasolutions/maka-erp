using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.SetPrimaryImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.SetPrimaryImage;

public static class SetPrimaryImageEndpoint
{
    public static RouteHandlerBuilder MapSetPrimaryImageEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{imageId:guid}/primary",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new SetPrimaryImageCommand(productId, imageId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("SetPrimaryImage")
            .WithSummary("Set primary product image")
            .WithDescription("Marks an image as the product's primary (cover) image.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
