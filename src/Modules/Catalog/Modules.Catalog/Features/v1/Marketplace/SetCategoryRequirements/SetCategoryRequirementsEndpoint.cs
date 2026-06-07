using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.SetCategoryRequirements;

public static class SetCategoryRequirementsEndpoint
{
    public static RouteHandlerBuilder MapSetCategoryRequirementsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{categoryId:guid}/marketplace-requirements",
                async (Guid categoryId, SetCategoryRequirementsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { CategoryId = categoryId };
                    int count = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(count);
                })
            .WithName("SetCategoryMarketplaceRequirements")
            .WithSummary("Set marketplace attribute requirements for a category")
            .WithDescription("Replaces the required-attribute set for one (category, marketplace).")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces<int>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
