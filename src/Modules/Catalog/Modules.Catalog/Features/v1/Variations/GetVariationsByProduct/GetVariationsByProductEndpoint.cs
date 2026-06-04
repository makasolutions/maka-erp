using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Variations.GetVariationsByProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Variations.GetVariationsByProduct;

public static class GetVariationsByProductEndpoint
{
    public static RouteHandlerBuilder MapGetVariationsByProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (Guid productId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetVariationsByProductQuery(productId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetVariationsByProduct")
            .WithSummary("List product variations")
            .WithDescription("Returns all variations for a product, including soft-deleted ones.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<VariationDto>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
