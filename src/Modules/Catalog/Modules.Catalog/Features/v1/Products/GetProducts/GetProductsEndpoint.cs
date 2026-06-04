using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.GetProducts;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProducts;

public static class GetProductsEndpoint
{
    public static RouteHandlerBuilder MapGetProductsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetProductsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetProducts")
            .WithSummary("List products")
            .WithDescription("Returns a paged list of products with optional filters.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<PagedResponse<ProductDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
