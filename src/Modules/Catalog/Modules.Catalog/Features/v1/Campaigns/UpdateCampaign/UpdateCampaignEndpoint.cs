using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.UpdateCampaign;

public static class UpdateCampaignEndpoint
{
    public static RouteHandlerBuilder MapUpdateCampaignEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateCampaignCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { Id = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateCampaign")
            .WithSummary("Edit a campaign and reschedule its start/end jobs")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
