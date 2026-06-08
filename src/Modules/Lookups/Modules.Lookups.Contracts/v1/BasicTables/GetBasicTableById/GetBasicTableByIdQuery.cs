using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTableById;

public sealed record GetBasicTableByIdQuery(Guid Id) : IQuery<BasicTableDetailDto>;
