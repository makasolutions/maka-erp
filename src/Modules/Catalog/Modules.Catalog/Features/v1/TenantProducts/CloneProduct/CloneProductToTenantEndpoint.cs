using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.CloneProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.CloneProduct;

public static class CloneProductToTenantEndpoint
{
    public static RouteHandlerBuilder MapCloneProductToTenantEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{canonicalProductId:guid}/clone",
                async (Guid canonicalProductId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(new CloneProductToTenantCommand(canonicalProductId), cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/tenant-products/{id}", id);
                })
            .WithName("CloneProductToTenant")
            .WithSummary("Clone canonical product to tenant")
            .WithDescription("Creates a tenant override for a canonical product (spec §5.7).")
            .RequirePermission(CatalogPermissions.Products.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
