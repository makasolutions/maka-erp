using FSH.Modules.Hr.Contracts.v1.Employees;
using FSH.Modules.Hr.Domain;

namespace FSH.Modules.Hr.Features;

internal static class EmployeeMapping
{
    public static Employee ToEntity(Guid partyId, EmployeeData d) => Employee.Create(
        partyId, d.MainCurrency, d.IsVendedor, d.IsCobrador, d.BranchCode, d.CostCenterCode, d.SeniorityDate, d.Notes,
        d.PayrollEnabled, d.HealthProviderCode, d.PensionFundCode, d.SeveranceFundCode, d.CcfCode, d.ArlProviderCode,
        d.PaymentMethodCode, d.LaborDepartmentCode, d.PositionCode, d.ImmediateBossPartyId, d.PositionStartDate,
        d.ContractTypeCode, d.ContractDurationCode, d.ContractStartDate, d.ArlRiskLevelCode, d.HighPensionRisk,
        d.RestDays, d.AppliesLaw1607, d.SalaryTypeCode, d.BaseSalary, d.SalaryStartDate, d.LegalTransportAllowance);

    public static void Apply(this Employee e, EmployeeData d) => e.Update(
        d.MainCurrency, d.IsVendedor, d.IsCobrador, d.BranchCode, d.CostCenterCode, d.SeniorityDate, d.Notes,
        d.PayrollEnabled, d.HealthProviderCode, d.PensionFundCode, d.SeveranceFundCode, d.CcfCode, d.ArlProviderCode,
        d.PaymentMethodCode, d.LaborDepartmentCode, d.PositionCode, d.ImmediateBossPartyId, d.PositionStartDate,
        d.ContractTypeCode, d.ContractDurationCode, d.ContractStartDate, d.ArlRiskLevelCode, d.HighPensionRisk,
        d.RestDays, d.AppliesLaw1607, d.SalaryTypeCode, d.BaseSalary, d.SalaryStartDate, d.LegalTransportAllowance);

    public static EmployeeData ToData(this Employee e) => new(
        e.MainCurrency, e.IsVendedor, e.IsCobrador, e.BranchCode, e.CostCenterCode, e.SeniorityDate, e.Notes,
        e.PayrollEnabled, e.HealthProviderCode, e.PensionFundCode, e.SeveranceFundCode, e.CcfCode, e.ArlProviderCode,
        e.PaymentMethodCode, e.LaborDepartmentCode, e.PositionCode, e.ImmediateBossPartyId, e.PositionStartDate,
        e.ContractTypeCode, e.ContractDurationCode, e.ContractStartDate, e.ArlRiskLevelCode, e.HighPensionRisk,
        e.RestDays, e.AppliesLaw1607, e.SalaryTypeCode, e.BaseSalary, e.SalaryStartDate, e.LegalTransportAllowance);
}
