using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.DeleteTaxRate;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.DeleteTaxRate;

public static class DeleteTaxRateEndpoint
{
    public static RouteHandlerBuilder MapDeleteTaxRateEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new DeleteTaxRateCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteTaxRate")
            .WithSummary("Delete tax rate")
            .WithDescription("Deletes a tax rate. Fails if it is assigned to any product.")
            .RequirePermission(CatalogPermissions.Settings.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
