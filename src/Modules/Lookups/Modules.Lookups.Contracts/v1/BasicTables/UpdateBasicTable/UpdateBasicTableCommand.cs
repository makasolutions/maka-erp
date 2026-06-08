using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.UpdateBasicTable;

public sealed record UpdateBasicTableCommand(
    Guid    Id,
    string  Name,
    string? Description,
    bool    IsManageable,
    int     SortOrder,
    bool    VisibleInMenu) : ICommand<Guid>;
