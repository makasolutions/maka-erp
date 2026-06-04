using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.GetProductCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.GetProductCodes;

public static class GetProductCodesEndpoint
{
    public static RouteHandlerBuilder MapGetProductCodesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (Guid productId, Guid variationId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetProductCodesQuery(productId, variationId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetProductCodes")
            .WithSummary("List codes for a variation")
            .WithDescription("Returns all product codes (SKU, EAN, UPC, SupplierCode…) for a specific variation.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<ProductCodeDto>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
