using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.RemoveProductCode;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.RemoveProductCode;

public static class RemoveProductCodeEndpoint
{
    public static RouteHandlerBuilder MapRemoveProductCodeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{codeId:guid}",
                async (Guid productId, Guid variationId, Guid codeId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RemoveProductCodeCommand(productId, variationId, codeId), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("RemoveProductCode")
            .WithSummary("Remove code from variation")
            .WithDescription("Permanently removes an identifier code from a variation.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
