using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Scorecards;

public sealed record GetScorecardKpisQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<ScorecardKpiDto>>;

public sealed record GetSupplierScorecardsQuery : IPagedQuery, IQuery<PagedResponse<ScorecardDto>>
{
    public int?             PageNumber { get; set; } = 1;
    public int?             PageSize   { get; set; } = 50;
    public string?          Sort       { get; set; }
    public string?          Search     { get; set; }
    public Guid?            SupplierId { get; set; }
    public ScorecardStatus? Status     { get; set; }
}

public sealed record GetScorecardByIdQuery(Guid Id) : IQuery<ScorecardDetailDto>;

public sealed record GetSupplierRankingQuery : IQuery<IReadOnlyList<SupplierRankingDto>>;

public sealed record GetSupplierScoreTrendQuery(Guid SupplierId) : IQuery<IReadOnlyList<ScoreTrendPointDto>>;
