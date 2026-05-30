using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductStats;

public static class GetProductStatsEndpoint
{
    internal static RouteHandlerBuilder MapGetProductStatsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/products/stats",
                (IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProductStatsQuery(), ct))
            .WithName("GetProductStats")
            .WithSummary("Catalog product counts (total, active, visible) for KPI cards")
            .RequirePermission(CatalogPermissions.Products.View);
    }
}
