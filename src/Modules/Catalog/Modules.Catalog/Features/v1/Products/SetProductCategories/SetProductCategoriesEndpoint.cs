using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductCategories;

public static class SetProductCategoriesEndpoint
{
    public static RouteHandlerBuilder MapSetProductCategoriesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}/categories",
                async (Guid id, SetProductCategoriesCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { ProductId = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("SetProductCategories")
            .WithSummary("Set product categories")
            .WithDescription("Replaces the product's category associations. Empty list clears them; exactly one may be primary.")
            .RequirePermission(CatalogPermissions.Products.Update)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
