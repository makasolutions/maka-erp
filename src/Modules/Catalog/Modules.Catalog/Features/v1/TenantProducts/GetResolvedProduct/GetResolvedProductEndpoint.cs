using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.GetResolvedProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.GetResolvedProduct;

public static class GetResolvedProductEndpoint
{
    public static RouteHandlerBuilder MapGetResolvedProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{canonicalProductId:guid}/resolved",
                async (Guid canonicalProductId, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetResolvedProductQuery(canonicalProductId), cancellationToken)))
            .WithName("GetResolvedProduct")
            .WithSummary("Get product resolved with tenant overrides")
            .WithDescription("Returns a canonical product merged with the current tenant's overrides (spec §5.7).")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<ResolvedProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
