using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Bundles.GetBundleItems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Bundles.GetBundleItems;

public static class GetBundleItemsEndpoint
{
    public static RouteHandlerBuilder MapGetBundleItemsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async (Guid productId, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetBundleItemsQuery(productId), cancellationToken)))
            .WithName("GetBundleItems")
            .WithSummary("List bundle items")
            .WithDescription("Returns the items (variations) included in a Bundle product (spec §2.13).")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<IReadOnlyList<BundleItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
