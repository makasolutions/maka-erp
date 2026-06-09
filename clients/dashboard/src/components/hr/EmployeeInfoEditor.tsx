import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import { Field, FormGrid } from "@/components/list";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { MakaCurrencyInput } from "@/components/maka";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { rules, validateSchema } from "@/lib/validation/rules";
import type { EmployeeData } from "@/api/hr";

export interface EmployeeInfoEditorProps {
  value: EmployeeData;
  onChange: (next: EmployeeData) => void;
  disabled?: boolean;
  /** Validation errors keyed by EmployeeData field (from validateEmployee). */
  errors?: Record<string, string>;
}

/**
 * Validates the labor/payroll fields by type (§17): ISO-4217 currency, non-negative
 * salary, rest-days code list, notes length, and chronological date relationships.
 * Returns `{ field: messageKey }` using the common `validation.*` namespace via `t`.
 */
export function validateEmployee(v: EmployeeData, t: TFunction): Record<string, string> {
  const e = validateSchema(
    {
      mainCurrency: v.mainCurrency ?? "",
      baseSalary: v.baseSalary ?? "",
      restDays: v.restDays ?? "",
      notes: v.notes ?? "",
    },
    {
      mainCurrency: [rules.pattern(/^[A-Za-z]{3}$/)],
      baseSalary: [rules.currency({ min: 0 })],
      restDays: [rules.pattern(/^[A-Za-zÁÉÍÓÚÑ]{2,9}(\s*,\s*[A-Za-zÁÉÍÓÚÑ]{2,9})*$/)],
      notes: [rules.max(2000)],
    },
    t,
  );
  // Cross-field: salary start must not precede contract start.
  if (v.salaryStartDate && v.contractStartDate && v.salaryStartDate < v.contractStartDate) {
    e.salaryStartDate = t("validation.dateRange");
  }
  return e;
}

export function EmployeeInfoEditor({ value: v, onChange, disabled, errors }: EmployeeInfoEditorProps) {
  const { t } = useTranslation("hr");
  const set = (patch: Partial<EmployeeData>) => onChange({ ...v, ...patch });
  const err = (k: string) => errors?.[k];

  return (
    <div className="space-y-6">
      {/* Datos básicos */}
      <section>
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("employee.sections.basic")}</h3>
        <FormGrid>
          <Field id="e-currency" span={3} label={t("employee.fields.mainCurrency")} error={err("mainCurrency")}>
            <Input id="e-currency" value={v.mainCurrency ?? ""} disabled={disabled} maxLength={3}
              onChange={(e) => set({ mainCurrency: e.target.value.toUpperCase() })} />
          </Field>
          <Field id="e-branch" span={3} label={t("employee.fields.branch")}>
            <BasicRecordSelect id="e-branch" tableCode="Branch" label={t("employee.fields.branch")}
              value={v.branchCode ?? null} onChange={(c) => set({ branchCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-cost" span={3} label={t("employee.fields.costCenter")}>
            <BasicRecordSelect id="e-cost" tableCode="CostCenter" label={t("employee.fields.costCenter")}
              value={v.costCenterCode ?? null} onChange={(c) => set({ costCenterCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-seniority" span={3} label={t("employee.fields.seniorityDate")}>
            <Input id="e-seniority" type="date" value={v.seniorityDate ?? ""} disabled={disabled}
              onChange={(e) => set({ seniorityDate: e.target.value || null })} />
          </Field>
          <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.isVendedor} disabled={disabled} onCheckedChange={(c) => set({ isVendedor: c })} />{t("employee.fields.isVendedor")}
            </label>
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.isCobrador} disabled={disabled} onCheckedChange={(c) => set({ isCobrador: c })} />{t("employee.fields.isCobrador")}
            </label>
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.payrollEnabled} disabled={disabled} onCheckedChange={(c) => set({ payrollEnabled: c })} />{t("employee.fields.payrollEnabled")}
            </label>
          </div>
          <Field id="e-eps" span={3} label={t("employee.fields.healthProvider")}>
            <BasicRecordSelect id="e-eps" tableCode="HealthProvider" label={t("employee.fields.healthProvider")}
              value={v.healthProviderCode ?? null} onChange={(c) => set({ healthProviderCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-afp" span={3} label={t("employee.fields.pensionFund")}>
            <BasicRecordSelect id="e-afp" tableCode="PensionFund" label={t("employee.fields.pensionFund")}
              value={v.pensionFundCode ?? null} onChange={(c) => set({ pensionFundCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-ces" span={3} label={t("employee.fields.severanceFund")}>
            <BasicRecordSelect id="e-ces" tableCode="SeveranceFund" label={t("employee.fields.severanceFund")}
              value={v.severanceFundCode ?? null} onChange={(c) => set({ severanceFundCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-ccf" span={3} label={t("employee.fields.ccf")}>
            <BasicRecordSelect id="e-ccf" tableCode="Ccf" label={t("employee.fields.ccf")}
              value={v.ccfCode ?? null} onChange={(c) => set({ ccfCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-arl" span={3} label={t("employee.fields.arlProvider")}>
            <BasicRecordSelect id="e-arl" tableCode="ArlProvider" label={t("employee.fields.arlProvider")}
              value={v.arlProviderCode ?? null} onChange={(c) => set({ arlProviderCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-pay" span={3} label={t("employee.fields.paymentMethod")}>
            <BasicRecordSelect id="e-pay" tableCode="PayrollPaymentMethod" label={t("employee.fields.paymentMethod")}
              value={v.paymentMethodCode ?? null} onChange={(c) => set({ paymentMethodCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-notes" span={12} label={t("employee.fields.notes")} error={err("notes")}>
            <Textarea id="e-notes" rows={2} value={v.notes ?? ""} disabled={disabled} onChange={(e) => set({ notes: e.target.value })} />
          </Field>
        </FormGrid>
      </section>

      {/* Cargo */}
      <section className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("employee.sections.position")}</h3>
        <FormGrid>
          <Field id="e-dept" span={4} label={t("employee.fields.laborDepartment")}>
            <BasicRecordSelect id="e-dept" tableCode="LaborDepartment" label={t("employee.fields.laborDepartment")}
              value={v.laborDepartmentCode ?? null} onChange={(c) => set({ laborDepartmentCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-pos" span={4} label={t("employee.fields.position")}>
            <BasicRecordSelect id="e-pos" tableCode="Position" label={t("employee.fields.position")}
              value={v.positionCode ?? null} onChange={(c) => set({ positionCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-posdate" span={4} label={t("employee.fields.positionStartDate")}>
            <Input id="e-posdate" type="date" value={v.positionStartDate ?? ""} disabled={disabled}
              onChange={(e) => set({ positionStartDate: e.target.value || null })} />
          </Field>
        </FormGrid>
      </section>

      {/* Contrato laboral */}
      <section className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("employee.sections.contract")}</h3>
        <FormGrid>
          <Field id="e-ctype" span={4} label={t("employee.fields.contractType")}>
            <BasicRecordSelect id="e-ctype" tableCode="EmployeeContractType" label={t("employee.fields.contractType")}
              value={v.contractTypeCode ?? null} onChange={(c) => set({ contractTypeCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-cdur" span={4} label={t("employee.fields.contractDuration")}>
            <BasicRecordSelect id="e-cdur" tableCode="ContractDuration" label={t("employee.fields.contractDuration")}
              value={v.contractDurationCode ?? null} onChange={(c) => set({ contractDurationCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-cdate" span={4} label={t("employee.fields.contractStartDate")}>
            <Input id="e-cdate" type="date" value={v.contractStartDate ?? ""} disabled={disabled}
              onChange={(e) => set({ contractStartDate: e.target.value || null })} />
          </Field>
          <Field id="e-risk" span={4} label={t("employee.fields.arlRiskLevel")}>
            <BasicRecordSelect id="e-risk" tableCode="ArlRiskLevel" label={t("employee.fields.arlRiskLevel")}
              value={v.arlRiskLevelCode ?? null} onChange={(c) => set({ arlRiskLevelCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-rest" span={4} label={t("employee.fields.restDays")} error={err("restDays")}>
            <Input id="e-rest" value={v.restDays ?? ""} disabled={disabled} placeholder="SAB,DOM"
              onChange={(e) => set({ restDays: e.target.value })} />
          </Field>
          <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.highPensionRisk} disabled={disabled} onCheckedChange={(c) => set({ highPensionRisk: c })} />{t("employee.fields.highPensionRisk")}
            </label>
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.appliesLaw1607} disabled={disabled} onCheckedChange={(c) => set({ appliesLaw1607: c })} />{t("employee.fields.appliesLaw1607")}
            </label>
          </div>
        </FormGrid>
      </section>

      {/* Salario */}
      <section className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("employee.sections.salary")}</h3>
        <FormGrid>
          <Field id="e-stype" span={4} label={t("employee.fields.salaryType")}>
            <BasicRecordSelect id="e-stype" tableCode="SalaryType" label={t("employee.fields.salaryType")}
              value={v.salaryTypeCode ?? null} onChange={(c) => set({ salaryTypeCode: c })} disabled={disabled} />
          </Field>
          <Field id="e-salary" span={4} label={t("employee.fields.baseSalary")} error={err("baseSalary")}>
            <MakaCurrencyInput id="e-salary" value={v.baseSalary ?? null} disabled={disabled}
              onChange={(n) => set({ baseSalary: n })} />
          </Field>
          <Field id="e-sdate" span={4} label={t("employee.fields.salaryStartDate")} error={err("salaryStartDate")}>
            <Input id="e-sdate" type="date" value={v.salaryStartDate ?? ""} disabled={disabled}
              onChange={(e) => set({ salaryStartDate: e.target.value || null })} />
          </Field>
          <div className="col-span-1 sm:col-span-12">
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.legalTransportAllowance} disabled={disabled} onCheckedChange={(c) => set({ legalTransportAllowance: c })} />{t("employee.fields.legalTransportAllowance")}
            </label>
          </div>
        </FormGrid>
      </section>
    </div>
  );
}
