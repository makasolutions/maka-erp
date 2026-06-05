using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Bundles.AddBundleItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Bundles.AddBundleItem;

public static class AddBundleItemEndpoint
{
    public static RouteHandlerBuilder MapAddBundleItemEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (Guid productId, AddBundleItemCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = productId };
                    Guid id = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/products/{productId}/bundle-items/{id}", id);
                })
            .WithName("AddBundleItem")
            .WithSummary("Add bundle item")
            .WithDescription("Adds a variation as an item of a Bundle product (spec §2.13).")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
