namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTableById;

public sealed record BasicTableDetailDto(
    Guid    Id,
    string  Code,
    string  Name,
    string? Description,
    bool    IsManageable,
    int     SortOrder,
    bool    VisibleInMenu,
    bool    IsGlobal,
    IReadOnlyList<BasicRecordDto> Records);

public sealed record BasicRecordDto(
    Guid   Id,
    string Code,
    string Value,
    int    SortOrder,
    bool   IsActive);
