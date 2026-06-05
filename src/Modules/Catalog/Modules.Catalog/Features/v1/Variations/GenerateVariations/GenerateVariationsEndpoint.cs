using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Variations.GenerateVariations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Variations.GenerateVariations;

public static class GenerateVariationsEndpoint
{
    public static RouteHandlerBuilder MapGenerateVariationsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/generate",
                async (Guid productId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GenerateVariationsCommand(productId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GenerateVariations")
            .WithSummary("Generate variations from attributes")
            .WithDescription("Creates one variation per attribute-value combination (cartesian product). Existing combinations are skipped (spec §5.4).")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<GenerateVariationsResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
