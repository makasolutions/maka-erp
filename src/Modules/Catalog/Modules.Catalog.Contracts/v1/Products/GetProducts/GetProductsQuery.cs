using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetProducts;

public sealed record GetProductsQuery : IPagedQuery, IQuery<PagedResponse<ProductDto>>
{
    public int?          PageNumber { get; set; } = 1;
    public int?          PageSize   { get; set; } = 50;
    public string?       Sort       { get; set; }
    public string?       Search     { get; set; }
    public Guid?         BrandId    { get; set; }
    public ProductType?  Type       { get; set; }
    public ProductStatus? Status    { get; set; }
}
