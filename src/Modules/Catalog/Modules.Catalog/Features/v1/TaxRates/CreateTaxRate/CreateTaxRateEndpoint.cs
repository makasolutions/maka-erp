using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.CreateTaxRate;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.CreateTaxRate;

public static class CreateTaxRateEndpoint
{
    public static RouteHandlerBuilder MapCreateTaxRateEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateTaxRateCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/tax-rates/{id}", id);
                })
            .WithName("CreateTaxRate")
            .WithSummary("Create tax rate")
            .WithDescription("Creates a tax rate (spec §2.3). Setting it as default clears the previous default.")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
