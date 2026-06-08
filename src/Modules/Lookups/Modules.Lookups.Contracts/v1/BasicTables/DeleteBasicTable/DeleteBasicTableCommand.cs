using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.DeleteBasicTable;

public sealed record DeleteBasicTableCommand(Guid Id) : ICommand;
