using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceListItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceListItem;

public static class UpdatePriceListItemEndpoint
{
    public static RouteHandlerBuilder MapUpdatePriceListItemEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/items/{itemId:guid}",
                async (Guid id, Guid itemId, UpdatePriceListItemCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { PriceListId = id, ItemId = itemId };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdatePriceListItem")
            .WithSummary("Update price list item")
            .WithDescription("Changes the price of an item. Records an immutable price-history entry.")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
