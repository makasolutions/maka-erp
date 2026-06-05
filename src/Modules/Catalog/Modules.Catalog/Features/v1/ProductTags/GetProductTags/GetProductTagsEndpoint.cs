using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductTags.GetProductTags;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductTags.GetProductTags;

public static class GetProductTagsEndpoint
{
    public static RouteHandlerBuilder MapGetProductTagsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (Guid productId, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetProductTagsQuery(productId), cancellationToken)))
            .WithName("GetProductTags")
            .WithSummary("List product tags")
            .WithDescription("Returns the tags assigned to a product.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<ProductTagDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
