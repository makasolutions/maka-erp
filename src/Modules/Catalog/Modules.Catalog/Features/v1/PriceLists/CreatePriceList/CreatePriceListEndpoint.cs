using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;

public static class CreatePriceListEndpoint
{
    public static RouteHandlerBuilder MapCreatePriceListEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreatePriceListCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/price-lists/{id}", id);
                })
            .WithName("CreatePriceList")
            .WithSummary("Create price list")
            .WithDescription("Creates a price list for a customer segment. Closes the previous active list of the same segment.")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
