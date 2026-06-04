using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.DeleteCategory;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory;

public static class DeleteCategoryEndpoint
{
    public static RouteHandlerBuilder MapDeleteCategoryEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteCategory")
            .WithSummary("Delete category (soft)")
            .WithDescription("Soft-deletes a category. Fails if it has active sub-categories or products.")
            .RequirePermission(CatalogPermissions.Categories.Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
