namespace FSH.Modules.Hr.Contracts.v1.Employees;

/// <summary>Conjunto completo de campos laborales/nómina de un empleado (sin identidad).</summary>
public sealed record EmployeeData(
    // Datos básicos
    string?   MainCurrency = null,
    bool      IsVendedor = false,
    bool      IsCobrador = false,
    string?   BranchCode = null,
    string?   CostCenterCode = null,
    DateOnly? SeniorityDate = null,
    string?   Notes = null,
    bool      PayrollEnabled = false,
    string?   HealthProviderCode = null,
    string?   PensionFundCode = null,
    string?   SeveranceFundCode = null,
    string?   CcfCode = null,
    string?   ArlProviderCode = null,
    string?   PaymentMethodCode = null,
    // Cargo
    string?   LaborDepartmentCode = null,
    string?   PositionCode = null,
    Guid?     ImmediateBossPartyId = null,
    DateOnly? PositionStartDate = null,
    // Contrato laboral
    string?   ContractTypeCode = null,
    string?   ContractDurationCode = null,
    DateOnly? ContractStartDate = null,
    string?   ArlRiskLevelCode = null,
    bool      HighPensionRisk = false,
    string?   RestDays = null,
    bool      AppliesLaw1607 = false,
    // Salario
    string?   SalaryTypeCode = null,
    decimal?  BaseSalary = null,
    DateOnly? SalaryStartDate = null,
    bool      LegalTransportAllowance = false);
