using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductAttributes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductAttributes;

public static class SetProductAttributesEndpoint
{
    public static RouteHandlerBuilder MapSetProductAttributesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/attributes",
                async (Guid id, SetProductAttributesCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("SetProductAttributes")
            .WithSummary("Set product attributes")
            .WithDescription("Replaces the product's attribute assignments and their selected values (spec §2.12).")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
