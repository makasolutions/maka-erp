using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands.ListTrashedBrands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;

public static class ListTrashedBrandsEndpoint
{
    public static RouteHandlerBuilder MapListTrashedBrandsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/trash",
                async ([AsParameters] ListTrashedBrandsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("ListTrashedBrands")
            .WithSummary("List trashed brands")
            .WithDescription("Returns a paginated list of soft-deleted brands.")
            .RequirePermission(CatalogPermissions.Brands.View)
            .Produces<PagedResponse<TrashedBrandDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
