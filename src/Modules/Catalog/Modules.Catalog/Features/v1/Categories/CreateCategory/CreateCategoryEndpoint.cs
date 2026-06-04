using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.CreateCategory;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.CreateCategory;

public static class CreateCategoryEndpoint
{
    public static RouteHandlerBuilder MapCreateCategoryEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateCategoryCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/categories/{id}", id);
                })
            .WithName("CreateCategory")
            .WithSummary("Create category")
            .WithDescription("Creates a new category for the current tenant.")
            .RequirePermission(CatalogPermissions.Categories.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
