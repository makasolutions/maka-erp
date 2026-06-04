using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.GetCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategories;

public static class GetCategoriesEndpoint
{
    public static RouteHandlerBuilder MapGetCategoriesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetCategoriesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("GetCategories")
            .WithSummary("Get category tree")
            .WithDescription("Returns the full category tree for the current tenant. Pass ParentId to get a subtree.")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
