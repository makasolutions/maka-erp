using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Hr.Contracts.v1.Employees;

public sealed record CreateEmployeeCommand(Guid PartyId, EmployeeData Data) : ICommand<Guid>;

public sealed record UpdateEmployeeCommand(Guid Id, EmployeeData Data) : ICommand<Guid>;

public sealed record DeleteEmployeeCommand(Guid Id) : ICommand;

public sealed record GetEmployeeByPartyIdQuery(Guid PartyId) : IQuery<EmployeeDetailDto>;

public sealed record GetEmployeesQuery : IPagedQuery, IQuery<PagedResponse<EmployeeDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public string? Search     { get; set; }   // PositionCode / BranchCode
    public bool?   PayrollEnabled { get; set; }
}
