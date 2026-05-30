using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

/// <summary>
/// Search for products with pagination and sorting.
/// </summary>
/// <param name="Search">Combined search term (matches name, SKU or slug).</param>
/// <param name="Sku">Optional SKU substring filter.</param>
/// <param name="Name">Optional name substring filter.</param>
/// <param name="BrandId">Optional brand filter.</param>
/// <param name="CategoryId">Optional category filter.</param>
/// <param name="IsActive">Optional active status filter.</param>
/// <param name="IsVisible">Optional visible status filter.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: name | sku | createdAtUtc | stock | price.</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchProductsQuery(
    string? Search = null,
    string? Sku = null,
    string? Name = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    bool? IsVisible = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<ProductDto>>;
