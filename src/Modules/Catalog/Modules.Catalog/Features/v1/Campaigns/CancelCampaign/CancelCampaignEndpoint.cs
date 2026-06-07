using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CancelCampaign;

public static class CancelCampaignEndpoint
{
    public static RouteHandlerBuilder MapCancelCampaignEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/cancel",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new CancelCampaignCommand(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("CancelCampaign")
            .WithSummary("Cancel a campaign and delete its scheduled jobs")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
