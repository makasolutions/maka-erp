using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.DuplicateProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.DuplicateProduct;

public static class DuplicateProductEndpoint
{
    public static RouteHandlerBuilder MapDuplicateProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/duplicate",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid newId = await mediator.Send(new DuplicateProductCommand(id), cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/products/{newId}", newId);
                })
            .WithName("DuplicateProduct")
            .WithSummary("Duplicate a product (codes cleared on the copy)")
            .RequirePermission(CatalogPermissions.Products.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
