using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.RejectPriceProposals;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.RejectPriceProposals;

public static class RejectPriceProposalsEndpoint
{
    public static RouteHandlerBuilder MapRejectPriceProposalsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{batchId:guid}/reject",
                async (Guid batchId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    int rejected = await mediator.Send(new RejectPriceProposalsCommand(batchId), cancellationToken);
                    return TypedResults.Ok(new { rejected });
                })
            .WithName("RejectPriceProposals")
            .WithSummary("Reject price proposals")
            .WithDescription("Rejects all pending proposals in a batch without changing any prices.")
            .RequirePermission(CatalogPermissions.PriceLists.ApproveBulk)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
