using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.BulkUpdatePrices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.BulkUpdatePrices;

public static class BulkUpdatePricesEndpoint
{
    public static RouteHandlerBuilder MapBulkUpdatePricesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/bulk-import",
                async (Guid id, BulkUpdatePricesFromCsvCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { PriceListId = id };
                    var result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("BulkUpdatePricesFromCsv")
            .WithSummary("Bulk price import from supplier CSV")
            .WithDescription("Parses a supplier CSV, matches SupplierCodes to variations, and creates pending price proposals.")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<BulkUpdatePricesResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
