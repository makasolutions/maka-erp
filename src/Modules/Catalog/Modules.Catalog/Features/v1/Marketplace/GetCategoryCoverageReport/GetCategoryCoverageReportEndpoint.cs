using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.GetCategoryCoverageReport;

public static class GetCategoryCoverageReportEndpoint
{
    public static RouteHandlerBuilder MapGetCategoryCoverageReportEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{categoryId:guid}/coverage",
                async (Guid categoryId, Marketplace? marketplace, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetCategoryCoverageReportQuery(categoryId, marketplace), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetCategoryCoverageReport")
            .WithSummary("Attribute coverage report for a category")
            .WithDescription("Which products in the category have each expected attribute (template or marketplace set).")
            .RequirePermission(CatalogPermissions.Attributes.View)
            .Produces<CategoryCoverageReportDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
