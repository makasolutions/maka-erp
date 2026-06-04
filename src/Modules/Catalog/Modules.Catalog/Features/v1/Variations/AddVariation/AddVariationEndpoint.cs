using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Variations.AddVariation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Variations.AddVariation;

public static class AddVariationEndpoint
{
    public static RouteHandlerBuilder MapAddVariationEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (Guid productId, AddVariationCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = productId };
                    Guid id = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/products/{productId}/variations/{id}", id);
                })
            .WithName("AddVariation")
            .WithSummary("Add variation to product")
            .WithDescription("Adds a new SKU variation to an existing product.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
