using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CreateCampaign;

public static class CreateCampaignEndpoint
{
    public static RouteHandlerBuilder MapCreateCampaignEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateCampaignCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/campaigns/{id}", id);
                })
            .WithName("CreateCampaign")
            .WithSummary("Create a campaign (offer) with validity + scheduled jobs")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
