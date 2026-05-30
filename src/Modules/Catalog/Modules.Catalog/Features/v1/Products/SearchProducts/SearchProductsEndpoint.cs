using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Products.SearchProducts;

public static class SearchProductsEndpoint
{
    internal static RouteHandlerBuilder MapSearchProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/products",
                (string? search, string? sku, string? name, Guid? brandId, Guid? categoryId, bool? isActive, bool? isVisible,
                 int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchProductsQuery(
                            Search: search,
                            Sku: sku,
                            Name: name,
                            BrandId: brandId,
                            CategoryId: categoryId,
                            IsActive: isActive,
                            IsVisible: isVisible,
                            PageNumber: pageNumber == 0 ? 1 : pageNumber,
                            PageSize: pageSize == 0 ? 20 : pageSize,
                            SortBy: sortBy,
                            SortDir: sortDir),
                        ct))
            .WithName("SearchProducts")
            .WithSummary("Search products (paged, filter by brand/category/active, sortable)")
            .RequirePermission(CatalogPermissions.Products.View);
    }
}
