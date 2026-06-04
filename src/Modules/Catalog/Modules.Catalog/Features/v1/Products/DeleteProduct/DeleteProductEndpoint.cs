using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.DeleteProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.DeleteProduct;

public static class DeleteProductEndpoint
{
    public static RouteHandlerBuilder MapDeleteProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteProductCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteProduct")
            .WithSummary("Delete product (soft)")
            .WithDescription("Soft-deletes a product and all its variations.")
            .RequirePermission(CatalogPermissions.Products.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
