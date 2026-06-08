using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.CreateBasicTable;

public sealed record CreateBasicTableCommand(
    string  Code,
    string  Name,
    string? Description,
    bool    IsManageable = true,
    int     SortOrder = 0,
    bool    VisibleInMenu = false,
    bool    IsGlobal = false) : ICommand<Guid>;
