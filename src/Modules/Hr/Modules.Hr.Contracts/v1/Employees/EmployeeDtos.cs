namespace FSH.Modules.Hr.Contracts.v1.Employees;

/// <summary>Fila de listado de empleados.</summary>
public sealed record EmployeeDto(
    Guid     Id,
    Guid     PartyId,
    string?  PositionCode,
    string?  LaborDepartmentCode,
    string?  BranchCode,
    bool     PayrollEnabled,
    decimal? BaseSalary,
    DateTime CreatedAtUtc);

/// <summary>Detalle completo de un empleado (incluye su data laboral).</summary>
public sealed record EmployeeDetailDto(
    Guid         Id,
    Guid         PartyId,
    EmployeeData Data);
