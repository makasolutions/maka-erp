using FSH.Modules.Catalog.Contracts.v1.Products.GetPublicProduct;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.GetPublicProduct;

public static class GetPublicProductEndpoint
{
    public static RouteHandlerBuilder MapGetPublicProductEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{slug}",
                [AllowAnonymous] async (string slug, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetPublicProductQuery(slug), cancellationToken)))
            .WithName("GetPublicProduct")
            .WithSummary("Get public product by slug")
            .WithDescription("Anonymous, shareable sheet for an Active product (tenant resolved from the 'tenant' header).")
            .AllowAnonymous()
            .Produces<PublicProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
}
