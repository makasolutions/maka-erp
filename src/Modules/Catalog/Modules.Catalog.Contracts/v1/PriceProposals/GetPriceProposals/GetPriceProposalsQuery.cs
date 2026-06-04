using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceProposals.GetPriceProposals;

public sealed record GetPriceProposalsQuery : IPagedQuery, IQuery<PagedResponse<PriceProposalDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public Guid?   BatchId    { get; set; }
    public PriceProposalStatus? Status { get; set; }
}
