using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands.GetBrandById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;

public static class GetBrandByIdEndpoint
{
    public static RouteHandlerBuilder MapGetBrandByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetBrandByIdQuery(id), cancellationToken)))
            .WithName("GetBrandById")
            .WithSummary("Get brand by ID")
            .WithDescription("Returns the brand with the given ID.")
            .RequirePermission(CatalogPermissions.Brands.View)
            .Produces<BrandDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
