using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryRequirements;

public static class GetCategoryRequirementsEndpoint
{
    public static RouteHandlerBuilder MapGetCategoryRequirementsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{categoryId:guid}/marketplace-requirements",
                async (Guid categoryId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetCategoryRequirementsQuery(categoryId), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetCategoryMarketplaceRequirements")
            .WithSummary("Get marketplace attribute requirements for a category")
            .RequirePermission(CatalogPermissions.Attributes.View)
            .Produces<CategoryRequirementsDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
