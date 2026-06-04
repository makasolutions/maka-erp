using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.ListTrashedProducts;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.ListTrashedProducts;

public static class ListTrashedProductsEndpoint
{
    public static RouteHandlerBuilder MapListTrashedProductsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/trash",
                async ([AsParameters] ListTrashedProductsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("ListTrashedProducts")
            .WithSummary("List trashed products")
            .WithDescription("Returns a paged list of soft-deleted products.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<PagedResponse<TrashedProductDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
