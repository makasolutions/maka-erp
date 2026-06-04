using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.AddProductCode;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.AddProductCode;

public static class AddProductCodeEndpoint
{
    public static RouteHandlerBuilder MapAddProductCodeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (Guid productId, Guid variationId, AddProductCodeCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = productId, VariationId = variationId };
                    Guid id = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created(
                        $"/api/v1/catalog/products/{productId}/variations/{variationId}/codes/{id}", id);
                })
            .WithName("AddProductCode")
            .WithSummary("Add code to variation")
            .WithDescription("Adds an identifier code (SKU, EAN, UPC, SupplierCode…) to a product variation.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
