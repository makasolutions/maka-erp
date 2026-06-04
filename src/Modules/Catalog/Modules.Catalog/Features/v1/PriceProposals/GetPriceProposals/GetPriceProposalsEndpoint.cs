using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.GetPriceProposals;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.GetPriceProposals;

public static class GetPriceProposalsEndpoint
{
    public static RouteHandlerBuilder MapGetPriceProposalsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetPriceProposalsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetPriceProposals")
            .WithSummary("List price proposals")
            .WithDescription("Returns a paged list of bulk price proposals, filterable by batch and status.")
            .RequirePermission(CatalogPermissions.PriceLists.View)
            .Produces<PagedResponse<PriceProposalDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
