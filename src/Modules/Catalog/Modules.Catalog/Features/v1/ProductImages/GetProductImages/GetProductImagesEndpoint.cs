using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.GetProductImages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.GetProductImages;

public static class GetProductImagesEndpoint
{
    public static RouteHandlerBuilder MapGetProductImagesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (Guid productId, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetProductImagesQuery(productId), cancellationToken)))
            .WithName("GetProductImages")
            .WithSummary("List product images")
            .WithDescription("Returns the gallery for a product, primary image first.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<ProductImageDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
