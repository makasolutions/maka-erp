using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.UpdateProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

public static class UpdateProductEndpoint
{
    public static RouteHandlerBuilder MapUpdateProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateProductCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { Id = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateProduct")
            .WithSummary("Update product")
            .WithDescription("Updates an existing product's details, shipping and SEO fields.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
