using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.GetBrands;

public sealed record GetBrandsQuery : IPagedQuery, IQuery<PagedResponse<BrandDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public string? Search     { get; set; }
    public bool?   IsActive   { get; set; }
}
