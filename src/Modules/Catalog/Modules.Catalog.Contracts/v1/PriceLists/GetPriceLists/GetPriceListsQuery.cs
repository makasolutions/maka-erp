using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceLists;

public sealed record GetPriceListsQuery : IPagedQuery, IQuery<PagedResponse<PriceListDto>>
{
    public int?     PageNumber      { get; set; } = 1;
    public int?     PageSize        { get; set; } = 50;
    public string?  Sort            { get; set; }
    public string?  Search          { get; set; }
    public string?  CustomerSegment { get; set; }
    public bool?    IsActive        { get; set; }
    public PriceListKind? Kind      { get; set; }
}
