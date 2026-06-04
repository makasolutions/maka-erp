using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.ArchiveProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.ArchiveProduct;

public static class ArchiveProductEndpoint
{
    public static RouteHandlerBuilder MapArchiveProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/archive",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new ArchiveProductCommand(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("ArchiveProduct")
            .WithSummary("Archive product")
            .WithDescription("Transitions a product to Archived status. Archived products are hidden from the catalog.")
            .RequirePermission(CatalogPermissions.Products.Archive)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
