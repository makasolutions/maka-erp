using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands.RestoreBrand;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand;

public static class RestoreBrandEndpoint
{
    public static RouteHandlerBuilder MapRestoreBrandEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new RestoreBrandCommand(id), cancellationToken)))
            .WithName("RestoreBrand")
            .WithSummary("Restore deleted brand")
            .WithDescription("Restores a soft-deleted brand.")
            .RequirePermission(CatalogPermissions.Brands.Restore)
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
