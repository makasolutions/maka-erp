using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.Records.UpsertBasicRecords;

/// <summary>Reemplaza/actualiza en lote los registros de una tabla básica (editor master-detail).</summary>
public sealed record UpsertBasicRecordsCommand(
    Guid Id,
    IReadOnlyList<BasicRecordInput> Records) : ICommand;

public sealed record BasicRecordInput(
    Guid?  Id,
    string Code,
    string Value,
    int    SortOrder = 0,
    bool   IsActive = true);
