using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.GetProductById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductById;

public static class GetProductByIdEndpoint
{
    public static RouteHandlerBuilder MapGetProductByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .WithDescription("Returns the full detail of a product including images, variations and categories.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<ProductDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
