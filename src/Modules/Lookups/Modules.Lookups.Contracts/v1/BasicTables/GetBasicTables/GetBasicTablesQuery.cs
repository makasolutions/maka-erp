using FSH.Framework.Shared.Persistence;
using FSH.Modules.Lookups.Contracts.v1.BasicTables;
using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTables;

public sealed record GetBasicTablesQuery : IPagedQuery, IQuery<PagedResponse<BasicTableDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public string? Search     { get; set; }
    public bool?   IsGlobal   { get; set; }
}
