namespace FSH.Modules.Lookups.Contracts.v1.BasicTables;

public sealed record BasicTableDto(
    Guid    Id,
    string  Code,
    string  Name,
    string? Description,
    bool    IsManageable,
    int     SortOrder,
    bool    VisibleInMenu,
    bool    IsGlobal,
    int     RecordCount,
    DateTime CreatedAtUtc);
