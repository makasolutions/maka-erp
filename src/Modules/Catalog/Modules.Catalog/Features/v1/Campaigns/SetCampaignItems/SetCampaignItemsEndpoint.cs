using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.SetCampaignItems;

public static class SetCampaignItemsEndpoint
{
    public static RouteHandlerBuilder MapSetCampaignItemsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/items",
                async (Guid id, SetCampaignItemsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { CampaignId = id };
                    int count = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(count);
                })
            .WithName("SetCampaignItems")
            .WithSummary("Set the products a campaign applies to")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<int>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
