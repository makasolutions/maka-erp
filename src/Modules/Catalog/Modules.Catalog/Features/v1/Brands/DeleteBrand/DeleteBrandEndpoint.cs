using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands.DeleteBrand;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand;

public static class DeleteBrandEndpoint
{
    public static RouteHandlerBuilder MapDeleteBrandEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new DeleteBrandCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteBrand")
            .WithSummary("Delete brand (soft)")
            .WithDescription("Soft-deletes a brand. Fails if the brand has active products.")
            .RequirePermission(CatalogPermissions.Brands.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
