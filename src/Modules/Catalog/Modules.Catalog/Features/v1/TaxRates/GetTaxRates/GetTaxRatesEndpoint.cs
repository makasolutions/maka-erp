using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.GetTaxRates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.GetTaxRates;

public static class GetTaxRatesEndpoint
{
    public static RouteHandlerBuilder MapGetTaxRatesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetTaxRatesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("GetTaxRates")
            .WithSummary("List tax rates")
            .WithDescription("Returns the configured tax rates (spec §2.3).")
            .RequirePermission(CatalogPermissions.Settings.View)
            .Produces<IReadOnlyList<TaxRateDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
