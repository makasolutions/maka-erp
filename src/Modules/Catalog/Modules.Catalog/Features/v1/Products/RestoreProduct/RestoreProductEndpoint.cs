using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.RestoreProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.RestoreProduct;

public static class RestoreProductEndpoint
{
    public static RouteHandlerBuilder MapRestoreProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new RestoreProductCommand(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("RestoreProduct")
            .WithSummary("Restore deleted product")
            .WithDescription("Restores a soft-deleted product.")
            .RequirePermission(CatalogPermissions.Products.Restore)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
