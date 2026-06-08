import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type EmployeeData = {
  // Datos básicos
  mainCurrency?: string | null;
  isVendedor: boolean;
  isCobrador: boolean;
  branchCode?: string | null;
  costCenterCode?: string | null;
  seniorityDate?: string | null;
  notes?: string | null;
  payrollEnabled: boolean;
  healthProviderCode?: string | null;
  pensionFundCode?: string | null;
  severanceFundCode?: string | null;
  ccfCode?: string | null;
  arlProviderCode?: string | null;
  paymentMethodCode?: string | null;
  // Cargo
  laborDepartmentCode?: string | null;
  positionCode?: string | null;
  immediateBossPartyId?: string | null;
  positionStartDate?: string | null;
  // Contrato laboral
  contractTypeCode?: string | null;
  contractDurationCode?: string | null;
  contractStartDate?: string | null;
  arlRiskLevelCode?: string | null;
  highPensionRisk: boolean;
  restDays?: string | null;
  appliesLaw1607: boolean;
  // Salario
  salaryTypeCode?: string | null;
  baseSalary?: number | null;
  salaryStartDate?: string | null;
  legalTransportAllowance: boolean;
};

export function emptyEmployeeData(): EmployeeData {
  return {
    mainCurrency: "COP", isVendedor: false, isCobrador: false, payrollEnabled: false,
    highPensionRisk: false, appliesLaw1607: false, legalTransportAllowance: false,
  };
}

export type EmployeeDto = {
  id: string;
  partyId: string;
  positionCode?: string | null;
  laborDepartmentCode?: string | null;
  branchCode?: string | null;
  payrollEnabled: boolean;
  baseSalary?: number | null;
  createdAtUtc: string;
};

export type EmployeeDetailDto = {
  id: string;
  partyId: string;
  data: EmployeeData;
};

export function getEmployees(params: { search?: string; payrollEnabled?: boolean; pageSize?: number; sort?: string }) {
  const q = new URLSearchParams();
  if (params.search) q.set("Search", params.search);
  if (params.payrollEnabled != null) q.set("PayrollEnabled", String(params.payrollEnabled));
  if (params.pageSize) q.set("PageSize", String(params.pageSize));
  if (params.sort) q.set("Sort", params.sort);
  return apiFetch<PagedResponse<EmployeeDto>>(`/api/v1/hr/employees?${q.toString()}`);
}

export function getEmployeeByPartyId(partyId: string) {
  return apiFetch<EmployeeDetailDto>(`/api/v1/hr/employees/by-party/${partyId}`);
}

export function createEmployee(partyId: string, data: EmployeeData) {
  return apiFetch<string>(`/api/v1/hr/employees`, { method: "POST", body: JSON.stringify({ partyId, data }) });
}

export function updateEmployee(id: string, data: EmployeeData) {
  return apiFetch<void>(`/api/v1/hr/employees/${id}`, { method: "PUT", body: JSON.stringify({ id, data }) });
}

export function deleteEmployee(id: string) {
  return apiFetch<void>(`/api/v1/hr/employees/${id}`, { method: "DELETE" });
}
