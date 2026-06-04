using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Prices.GetEffectivePrice;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetEffectivePrice;

public static class GetEffectivePriceEndpoint
{
    public static RouteHandlerBuilder MapGetEffectivePriceEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/effective",
                async ([AsParameters] GetEffectivePriceQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetEffectivePrice")
            .WithSummary("Resolve effective price")
            .WithDescription("Resolves the effective price for a variation in a customer segment (falls back to retail).")
            .RequirePermission(CatalogPermissions.PriceLists.View)
            .Produces<EffectivePriceDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
