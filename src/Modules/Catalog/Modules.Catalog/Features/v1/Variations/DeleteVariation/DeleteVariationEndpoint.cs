using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Variations.DeleteVariation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Variations.DeleteVariation;

public static class DeleteVariationEndpoint
{
    public static RouteHandlerBuilder MapDeleteVariationEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid productId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteVariationCommand(productId, id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteVariation")
            .WithSummary("Delete variation (soft)")
            .WithDescription("Soft-deletes a variation. Cannot delete the default variation.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
