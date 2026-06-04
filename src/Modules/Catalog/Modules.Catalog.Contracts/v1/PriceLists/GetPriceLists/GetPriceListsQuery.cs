using FSH.Framework.Shared.Persistence;
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
}
