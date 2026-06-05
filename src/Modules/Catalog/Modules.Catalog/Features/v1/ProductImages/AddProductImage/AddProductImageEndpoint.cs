using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;

public static class AddProductImageEndpoint
{
    public static RouteHandlerBuilder MapAddProductImageEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (Guid productId, AddProductImageCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = productId };
                    Guid id = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/products/{productId}/images/{id}", id);
                })
            .WithName("AddProductImage")
            .WithSummary("Add product image")
            .WithDescription("Adds an image to a product's gallery. The first image becomes primary automatically.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
