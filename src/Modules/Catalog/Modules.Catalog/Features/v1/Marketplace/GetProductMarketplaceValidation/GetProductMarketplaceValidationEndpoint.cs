using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetProductMarketplaceValidation;

public static class GetProductMarketplaceValidationEndpoint
{
    public static RouteHandlerBuilder MapGetProductMarketplaceValidationEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}/marketplace-validation",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetProductMarketplaceValidationQuery(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetProductMarketplaceValidation")
            .WithSummary("Validate a product's attributes against marketplace requirements")
            .WithDescription("Non-blocking: lists required attributes still missing per marketplace.")
            .RequirePermission(CatalogPermissions.Products.View)
            .Produces<ProductMarketplaceValidationDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
