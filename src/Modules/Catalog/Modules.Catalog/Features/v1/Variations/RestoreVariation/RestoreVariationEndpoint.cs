using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Variations.RestoreVariation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Variations.RestoreVariation;

public static class RestoreVariationEndpoint
{
    public static RouteHandlerBuilder MapRestoreVariationEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/restore",
                async (Guid productId, Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new RestoreVariationCommand(productId, id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("RestoreVariation")
            .WithSummary("Restore deleted variation")
            .WithDescription("Restores a soft-deleted product variation.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
