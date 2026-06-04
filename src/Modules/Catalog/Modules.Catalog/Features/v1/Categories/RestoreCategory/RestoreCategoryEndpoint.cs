using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.RestoreCategory;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.RestoreCategory;

public static class RestoreCategoryEndpoint
{
    public static RouteHandlerBuilder MapRestoreCategoryEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid result = await mediator.Send(new RestoreCategoryCommand(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("RestoreCategory")
            .WithSummary("Restore deleted category")
            .WithDescription("Restores a soft-deleted category.")
            .RequirePermission(CatalogPermissions.Categories.Restore)
            .Produces<Guid>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
