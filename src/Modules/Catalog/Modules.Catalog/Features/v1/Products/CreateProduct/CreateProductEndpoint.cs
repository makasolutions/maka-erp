using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products.CreateProduct;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.CreateProduct;

public static class CreateProductEndpoint
{
    public static RouteHandlerBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateProductCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/products/{id}", id);
                })
            .WithName("CreateProduct")
            .WithSummary("Create product")
            .WithDescription("Creates a new product. For Simple and Service types, a default variation is created automatically.")
            .RequirePermission(CatalogPermissions.Products.Create)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
