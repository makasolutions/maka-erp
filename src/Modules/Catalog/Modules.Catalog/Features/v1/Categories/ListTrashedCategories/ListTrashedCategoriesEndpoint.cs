using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.ListTrashedCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;

public static class ListTrashedCategoriesEndpoint
{
    public static RouteHandlerBuilder MapListTrashedCategoriesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/trash",
                async ([AsParameters] ListTrashedCategoriesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("ListTrashedCategories")
            .WithSummary("List trashed categories")
            .WithDescription("Returns a paged list of soft-deleted categories.")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<PagedResponse<TrashedCategoryDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
