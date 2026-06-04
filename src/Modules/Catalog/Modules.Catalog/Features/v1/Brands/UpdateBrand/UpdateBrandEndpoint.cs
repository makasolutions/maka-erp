using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Brands.UpdateBrand;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;

public static class UpdateBrandEndpoint
{
    public static RouteHandlerBuilder MapUpdateBrandEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateBrandRequest request, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var command = new UpdateBrandCommand(
                        id,
                        request.Name,
                        request.Slug,
                        request.Description,
                        request.LogoUrl,
                        request.WebsiteUrl,
                        request.CountryOfOrigin,
                        request.IsActive);

                    Guid result = await mediator.Send(command, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateBrand")
            .WithSummary("Update brand")
            .WithDescription("Updates an existing brand.")
            .RequirePermission(CatalogPermissions.Brands.Update)
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}

public sealed record UpdateBrandRequest(
    string  Name,
    string? Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? CountryOfOrigin,
    bool    IsActive);
