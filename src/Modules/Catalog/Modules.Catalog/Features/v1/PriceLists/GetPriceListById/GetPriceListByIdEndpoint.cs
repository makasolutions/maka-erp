using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceListById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceListById;

public static class GetPriceListByIdEndpoint
{
    public static RouteHandlerBuilder MapGetPriceListByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetPriceListByIdQuery(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetPriceListById")
            .WithSummary("Get price list by ID")
            .WithDescription("Returns a price list with all its items (variation SKU + prices).")
            .RequirePermission(CatalogPermissions.PriceLists.View)
            .Produces<PriceListDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
