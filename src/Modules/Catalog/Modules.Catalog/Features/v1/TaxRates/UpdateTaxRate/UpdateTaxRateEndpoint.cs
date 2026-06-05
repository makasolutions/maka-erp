using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.UpdateTaxRate;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.UpdateTaxRate;

public static class UpdateTaxRateEndpoint
{
    public static RouteHandlerBuilder MapUpdateTaxRateEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateTaxRateCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { Id = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateTaxRate")
            .WithSummary("Update tax rate")
            .WithDescription("Updates a tax rate (spec §2.3).")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
