using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.PublishProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.PublishProduct;

public static class PublishProductEndpoint
{
    public static RouteHandlerBuilder MapPublishProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/publish",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new PublishProductCommand(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("PublishProduct")
            .WithSummary("Publish product")
            .WithDescription("Transitions a product from Draft to Active. Requires at least one active variation for Simple/Service types.")
            .RequirePermission(CatalogPermissions.Products.Publish)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
