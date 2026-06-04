using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Prices.GetPriceHistory;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetPriceHistory;

public static class GetPriceHistoryEndpoint
{
    public static RouteHandlerBuilder MapGetPriceHistoryEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/history/{variationId:guid}",
                async (Guid variationId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetPriceHistoryQuery(variationId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetPriceHistory")
            .WithSummary("Price change history")
            .WithDescription("Returns the immutable price-change history for a variation, newest first.")
            .RequirePermission(CatalogPermissions.PriceLists.View)
            .Produces<IReadOnlyList<PriceHistoryEntryDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
