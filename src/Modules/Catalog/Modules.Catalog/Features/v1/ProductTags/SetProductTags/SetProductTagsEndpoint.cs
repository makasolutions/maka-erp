using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.ProductTags.SetProductTags;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.ProductTags.SetProductTags;

public static class SetProductTagsEndpoint
{
    public static RouteHandlerBuilder MapSetProductTagsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/",
                async (Guid productId, SetProductTagsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = productId };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("SetProductTags")
            .WithSummary("Set product tags")
            .WithDescription("Replaces the product's tags (spec §2.10). Empty list clears them.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
