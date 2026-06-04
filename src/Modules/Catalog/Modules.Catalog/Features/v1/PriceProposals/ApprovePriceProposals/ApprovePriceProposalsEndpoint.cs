using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.ApprovePriceProposals;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.ApprovePriceProposals;

public static class ApprovePriceProposalsEndpoint
{
    public static RouteHandlerBuilder MapApprovePriceProposalsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{batchId:guid}/approve",
                async (Guid batchId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    int applied = await mediator.Send(new ApprovePriceProposalsCommand(batchId), cancellationToken);
                    return TypedResults.Ok(new { applied });
                })
            .WithName("ApprovePriceProposals")
            .WithSummary("Approve price proposals")
            .WithDescription("Applies all pending proposals in a batch: updates/creates price items and records history.")
            .RequirePermission(CatalogPermissions.PriceLists.ApproveBulk)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
