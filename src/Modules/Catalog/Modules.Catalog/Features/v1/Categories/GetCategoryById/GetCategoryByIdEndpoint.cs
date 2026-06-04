using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Categories.GetCategoryById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Categories.GetCategoryById;

public static class GetCategoryByIdEndpoint
{
    public static RouteHandlerBuilder MapGetCategoryByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetCategoryById")
            .WithSummary("Get category by ID")
            .WithDescription("Returns the detail of a single category.")
            .RequirePermission(CatalogPermissions.Categories.View)
            .Produces<CategoryDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
