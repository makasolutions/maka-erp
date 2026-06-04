using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.AddPriceListItem;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.AddPriceListItem;

public static class AddPriceListItemEndpoint
{
    public static RouteHandlerBuilder MapAddPriceListItemEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/items",
                async (Guid id, AddPriceListItemCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { PriceListId = id };
                    Guid itemId = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/price-lists/{id}/items/{itemId}", itemId);
                })
            .WithName("AddPriceListItem")
            .WithSummary("Add item to price list")
            .WithDescription("Sets the price for a variation within a price list.")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
