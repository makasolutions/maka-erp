using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.Records.DeleteBasicRecord;

public sealed record DeleteBasicRecordCommand(Guid TableId, Guid RecordId) : ICommand;
