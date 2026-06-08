import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Field, FormGrid, Combobox } from "@/components/list";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Button } from "@/components/ui/button";
import { MakaCurrencyInput } from "@/components/maka";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { AddressEditor } from "./AddressEditor";
import { ChannelEditor } from "./ChannelEditor";
import { ContactEditor } from "./ContactEditor";
import { nitVerificationDigit } from "@/lib/nit";
import { formatMoney } from "@/lib/list-helpers";
import { toast } from "sonner";
import {
  rolesToApi, verifyIdentification, type LifecycleStage, type PartyAddress, type PartyChannel, type PartyContact,
  type PartyDetailDto, type PartyKind, type PartyRoles, type PartyStatus, type PartyWriteInput,
} from "@/api/parties";

export type PartyFormValue = {
  identificationTypeCode: string | null;
  identificationNumber: string;
  verificationDigit: number | null;
  kind: PartyKind;
  legalName: string;
  firstName: string;
  lastName: string;
  tradeName: string;
  customer: boolean;
  supplier: boolean;
  email: string;
  website: string;
  taxRegimeCode: string | null;
  fiscalResponsibilities: string | null;
  actividadEconomicaCiiuCode: string | null;
  status: PartyStatus;
  stage: LifecycleStage;
  leadScore: number;
  sourceCode: string | null;
  hasCredit: boolean;
  creditLimit: number | null;
  creditDaysCode: string | null;
  creditBlocked: boolean;
  notes: string;
  addresses: PartyAddress[];
  contacts: PartyContact[];
  channels: PartyChannel[];
};

export function emptyPartyForm(): PartyFormValue {
  return {
    identificationTypeCode: null, identificationNumber: "", verificationDigit: null, kind: "Juridica",
    legalName: "", firstName: "", lastName: "", tradeName: "", customer: true, supplier: false, email: "", website: "",
    taxRegimeCode: null, fiscalResponsibilities: null, actividadEconomicaCiiuCode: null,
    status: "Active", stage: "Lead", leadScore: 0, sourceCode: null,
    hasCredit: false, creditLimit: null, creditDaysCode: "30", creditBlocked: false,
    notes: "", addresses: [emptyAddress()], contacts: [emptyContact()], channels: [],
  };
}

export function emptyAddress(isPrimary = true): PartyAddress {
  return { country: "Colombia", isPrimary, department: null, city: null, line: "", labelCode: null };
}
export function emptyContact(): PartyContact {
  return { reference: "", isCommercial: false };
}
/** A contact with no identifying data at all — dropped before submit. */
export function isContactBlank(c: PartyContact): boolean {
  return ![c.reference, c.firstName, c.lastName, c.email, c.cell, c.identificationNumber]
    .some((x) => (x ?? "").trim().length > 0);
}

function rolesHas(roles: PartyRoles, r: string) { return roles.includes(r); }

export function partyFormFromDetail(d: PartyDetailDto): PartyFormValue {
  return {
    identificationTypeCode: d.identificationTypeCode, identificationNumber: d.identificationNumber,
    verificationDigit: d.verificationDigit ?? null, kind: d.kind, legalName: d.legalName,
    firstName: d.firstName ?? "", lastName: d.lastName ?? "", tradeName: d.tradeName ?? "",
    customer: rolesHas(d.roles, "Customer"), supplier: rolesHas(d.roles, "Supplier"),
    email: d.email ?? "", website: d.website ?? "", taxRegimeCode: d.taxRegimeCode ?? null,
    fiscalResponsibilities: d.fiscalResponsibilities ?? null, actividadEconomicaCiiuCode: d.actividadEconomicaCiiuCode ?? null,
    status: d.status, stage: d.stage, leadScore: d.leadScore, sourceCode: d.sourceCode ?? null,
    hasCredit: d.hasCredit, creditLimit: d.creditLimit ?? null, creditDaysCode: d.creditDaysCode ?? null,
    creditBlocked: d.creditBlocked, notes: d.notes ?? "",
    addresses: d.addresses, contacts: d.contacts, channels: d.channels,
  };
}

export function partyFormToInput(v: PartyFormValue): PartyWriteInput {
  const legalName = v.kind === "Natural" ? `${v.firstName.trim()} ${v.lastName.trim()}`.trim() : v.legalName.trim();
  return {
    identificationTypeCode: v.identificationTypeCode ?? "", identificationNumber: v.identificationNumber.trim(),
    verificationDigit: v.verificationDigit, kind: v.kind, legalName: legalName || v.legalName.trim() || "—",
    firstName: v.firstName.trim() || null, lastName: v.lastName.trim() || null,
    roles: rolesToApi(v.customer, v.supplier), tradeName: v.tradeName.trim() || null, email: v.email.trim() || null,
    website: v.website.trim() || null, taxRegimeCode: v.taxRegimeCode, fiscalResponsibilities: v.fiscalResponsibilities,
    actividadEconomicaCiiuCode: v.actividadEconomicaCiiuCode,
    status: v.status, stage: v.stage, leadScore: v.leadScore, sourceCode: v.sourceCode, marketingType: null,
    birthDate: null, genderCode: null, maritalStatusCode: null,
    hasCredit: v.hasCredit, creditLimit: v.creditLimit, creditDaysCode: v.creditDaysCode, creditBlocked: v.creditBlocked,
    creditCurrency: null, notes: v.notes.trim() || null, branchId: null,
    addresses: v.addresses, contacts: v.contacts.filter((c) => !isContactBlank(c)), channels: v.channels, team: [],
  };
}

type TabId = "id" | "contacts" | "crm" | "finance" | "tax";

export interface PartyFormProps {
  value: PartyFormValue;
  onChange: (next: PartyFormValue) => void;
  isCreate: boolean;
  disabled?: boolean;
}

export function PartyForm({ value: v, onChange, isCreate, disabled }: PartyFormProps) {
  const { t } = useTranslation("crm");
  const [tab, setTab] = useState<TabId>("id");
  const [verifying, setVerifying] = useState(false);
  const set = (patch: Partial<PartyFormValue>) => onChange({ ...v, ...patch });

  const onVerify = async () => {
    if (!v.identificationTypeCode || !v.identificationNumber.trim()) return;
    setVerifying(true);
    try {
      const r = await verifyIdentification(v.identificationTypeCode, v.identificationNumber.trim(), v.verificationDigit);
      if (!r.valid) { toast.error(r.error ?? t("parties.verify.invalid")); return; }
      const patch: Partial<PartyFormValue> = {};
      if (r.verificationDigit != null) patch.verificationDigit = r.verificationDigit;
      if (r.legalName && v.kind === "Juridica") patch.legalName = r.legalName;
      if (Object.keys(patch).length) set(patch);
      toast.success(r.legalName ? t("parties.verify.found", { name: r.legalName }) : t("parties.verify.ok"));
    } catch {
      toast.error(t("parties.verify.failed"));
    } finally {
      setVerifying(false);
    }
  };

  const statusOpts: PartyStatus[] = ["Active", "Inactive", "Prospect"];
  const stageOpts: LifecycleStage[] = ["Lead", "Mql", "Sql", "Opportunity", "Customer", "Inactive"];
  const isNit = (v.identificationTypeCode ?? "") === "NIT";
  const debe = 0; // placeholder hasta CxC/CxP
  const cupo = (v.creditLimit ?? 0) - debe;

  const tabs: { id: TabId; label: string }[] = [
    { id: "id", label: t("parties.tabs.identity") },
    { id: "contacts", label: t("parties.tabs.contacts") },
    { id: "crm", label: t("parties.tabs.crm") },
    { id: "finance", label: t("parties.tabs.finance") },
    { id: "tax", label: t("parties.tabs.tax") },
  ];

  return (
    <div className="space-y-4">
      {/* Tab bar */}
      <div className="flex flex-wrap gap-1 border-b border-[var(--color-border)]">
        {tabs.map((tb) => (
          <button key={tb.id} type="button" onClick={() => setTab(tb.id)}
            className={`-mb-px rounded-t-lg border-b-2 px-3 py-2 text-[13px] font-semibold transition-colors ${
              tab === tb.id ? "border-[var(--color-primary)] text-[var(--color-foreground)]"
                : "border-transparent text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"}`}>
            {tb.label}
          </button>
        ))}
      </div>

      <div className="min-h-[480px]">
      {/* Tab 1 — Identificación + Direcciones + Canales */}
      {tab === "id" && (
        <div className="space-y-6">
          <FormGrid>
            <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
              <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("parties.fields.type")}</span>
              <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={v.customer} disabled={disabled} onCheckedChange={(c) => set({ customer: c })} />{t("parties.role.customer")}
              </label>
              <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={v.supplier} disabled={disabled} onCheckedChange={(c) => set({ supplier: c })} />{t("parties.role.supplier")}
              </label>
            </div>
            <Field id="p-kind" span={3} label={t("parties.fields.kind")} required>
              <Combobox id="p-kind" label={t("parties.fields.kind")} value={v.kind} onChange={(k) => k && set({ kind: k as PartyKind })}
                options={[{ value: "Juridica", label: t("parties.kind.Juridica") }, { value: "Natural", label: t("parties.kind.Natural") }]} disabled={disabled} />
            </Field>
            <Field id="p-idtype" span={3} label={t("parties.fields.idType")} required>
              <BasicRecordSelect id="p-idtype" tableCode="IdentificationType" label={t("parties.fields.idType")}
                value={v.identificationTypeCode} onChange={(c) => set({ identificationTypeCode: c })} disabled={disabled || !isCreate} />
            </Field>
            <Field id="p-idnum" span={4} label={t("parties.fields.idNumber")} required>
              <Input id="p-idnum" value={v.identificationNumber} disabled={disabled || !isCreate} className="font-mono"
                onChange={(e) => set({ identificationNumber: e.target.value })} />
            </Field>
            <Field id="p-dv" span={2} label={t("parties.fields.dv")}>
              <div className="flex gap-1">
                <Input id="p-dv" type="number" className="font-mono" value={v.verificationDigit ?? ""} disabled={disabled}
                  onChange={(e) => set({ verificationDigit: e.target.value === "" ? null : Number(e.target.value) })} />
                {isNit && !disabled && (
                  <Button type="button" variant="outline" size="sm" title={t("parties.fields.dvCompute")}
                    onClick={() => set({ verificationDigit: nitVerificationDigit(v.identificationNumber) })}>DV</Button>
                )}
              </div>
            </Field>
            {v.kind === "Juridica" ? (
              <>
                <Field id="p-legal" span={8} label={t("parties.fields.legalName")} required>
                  <Input id="p-legal" value={v.legalName} disabled={disabled} onChange={(e) => set({ legalName: e.target.value })} />
                </Field>
                <Field id="p-trade" span={4} label={t("parties.fields.tradeName")}>
                  <Input id="p-trade" value={v.tradeName} disabled={disabled} onChange={(e) => set({ tradeName: e.target.value })} />
                </Field>
              </>
            ) : (
              <>
                <Field id="p-first" span={6} label={t("parties.fields.firstName")} required>
                  <Input id="p-first" value={v.firstName} disabled={disabled} onChange={(e) => set({ firstName: e.target.value })} />
                </Field>
                <Field id="p-last" span={6} label={t("parties.fields.lastName")} required>
                  <Input id="p-last" value={v.lastName} disabled={disabled} onChange={(e) => set({ lastName: e.target.value })} />
                </Field>
              </>
            )}
            <Field id="p-email" span={6} label={t("parties.fields.email")}>
              <Input id="p-email" type="email" value={v.email} disabled={disabled} onChange={(e) => set({ email: e.target.value })} />
            </Field>
            <Field id="p-web" span={6} label={t("parties.fields.website")}>
              <Input id="p-web" value={v.website} disabled={disabled} onChange={(e) => set({ website: e.target.value })} />
            </Field>
            {!disabled && (
              <div className="col-span-1 sm:col-span-12">
                <Button type="button" variant="outline" size="sm" disabled={verifying || !v.identificationTypeCode || !v.identificationNumber.trim()}
                  onClick={onVerify}>
                  {verifying ? t("parties.verify.verifying") : t("parties.verify.button")}
                </Button>
                <span className="ml-2 text-[11.5px] text-[var(--color-muted-foreground)]">{t("parties.verify.hint")}</span>
              </div>
            )}
          </FormGrid>

          <div className="border-t border-[var(--color-border)] pt-4">
            <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("parties.address.title")}<span className="ml-1 text-[var(--color-destructive)]">*</span></h3>
            <AddressEditor value={v.addresses} onChange={(a) => set({ addresses: a })} disabled={disabled} />
          </div>
          <div className="border-t border-[var(--color-border)] pt-4">
            <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("parties.channel.title")}</h3>
            <ChannelEditor value={v.channels} onChange={(c) => set({ channels: c })} disabled={disabled} />
          </div>
        </div>
      )}

      {/* Tab 2 — Contactos */}
      {tab === "contacts" && (
        <ContactEditor value={v.contacts} onChange={(c) => set({ contacts: c })} disabled={disabled} />
      )}

      {/* Tab 3 — CRM */}
      {tab === "crm" && (
        <FormGrid>
          <Field id="p-status" span={3} label={t("parties.fields.status")}>
            <Combobox id="p-status" label={t("parties.fields.status")} value={v.status} onChange={(s) => s && set({ status: s as PartyStatus })}
              options={statusOpts.map((s) => ({ value: s, label: t(`parties.status.${s}`) }))} disabled={disabled} />
          </Field>
          <Field id="p-stage" span={3} label={t("parties.fields.stage")}>
            <Combobox id="p-stage" label={t("parties.fields.stage")} value={v.stage} onChange={(s) => s && set({ stage: s as LifecycleStage })}
              options={stageOpts.map((s) => ({ value: s, label: t(`parties.stage.${s}`) }))} disabled={disabled} />
          </Field>
          <Field id="p-source" span={3} label={t("parties.fields.source")}>
            <BasicRecordSelect id="p-source" tableCode="PartySource" label={t("parties.fields.source")}
              value={v.sourceCode} onChange={(c) => set({ sourceCode: c })} disabled={disabled} />
          </Field>
          <Field id="p-score" span={3} label={t("parties.fields.leadScore")}>
            <Input id="p-score" type="number" value={String(v.leadScore)} disabled={disabled}
              onChange={(e) => set({ leadScore: Number(e.target.value) || 0 })} />
          </Field>
          <Field id="p-notes" span={12} label={t("parties.fields.notes")}>
            <Textarea id="p-notes" rows={3} value={v.notes} disabled={disabled} onChange={(e) => set({ notes: e.target.value })} />
          </Field>
        </FormGrid>
      )}

      {/* Tab 4 — Financiera */}
      {tab === "finance" && (
        <FormGrid>
          <div className="col-span-1 flex flex-wrap items-center gap-8 sm:col-span-12">
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.hasCredit} disabled={disabled} onCheckedChange={(c) => set({ hasCredit: c })} />{t("parties.finance.hasCredit")}
            </label>
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={v.creditBlocked} disabled={disabled} onCheckedChange={(c) => set({ creditBlocked: c })} />{t("parties.finance.blocked")}
            </label>
          </div>
          <Field id="p-climit" span={6} label={t("parties.finance.creditLimit")} hint={t("parties.finance.creditLimitHint")}>
            <MakaCurrencyInput id="p-climit" value={v.creditLimit} disabled={disabled || !v.hasCredit}
              onChange={(n) => set({ creditLimit: n })} />
          </Field>
          <Field id="p-cdays" span={6} label={t("parties.finance.creditDays")}>
            <BasicRecordSelect id="p-cdays" tableCode="CreditDays" label={t("parties.finance.creditDays")}
              value={v.creditDaysCode} onChange={(c) => set({ creditDaysCode: c })} disabled={disabled || !v.hasCredit} />
          </Field>
          <Field id="p-debe" span={6} label={t("parties.finance.debe")} hint={t("parties.finance.computedHint")}>
            <Input id="p-debe" value={formatMoney(debe)} disabled readOnly className="tabular-nums" />
          </Field>
          <Field id="p-cupo" span={6} label={t("parties.finance.available")} hint={t("parties.finance.computedHint")}>
            <Input id="p-cupo" value={formatMoney(cupo)} disabled readOnly className="tabular-nums" />
          </Field>
        </FormGrid>
      )}

      {/* Tab 5 — Tributaria */}
      {tab === "tax" && (
        <FormGrid>
          <Field id="p-regime" span={4} label={t("parties.fields.taxRegime")}>
            <BasicRecordSelect id="p-regime" tableCode="TaxRegime" label={t("parties.fields.taxRegime")}
              value={v.taxRegimeCode} onChange={(c) => set({ taxRegimeCode: c })} disabled={disabled} />
          </Field>
          <Field id="p-fiscal" span={4} label={t("parties.fields.fiscalResp")}>
            <BasicRecordSelect id="p-fiscal" tableCode="FiscalResponsibility" label={t("parties.fields.fiscalResp")}
              value={v.fiscalResponsibilities} onChange={(c) => set({ fiscalResponsibilities: c })} disabled={disabled} />
          </Field>
          <Field id="p-ciiu" span={4} label={t("parties.fields.ciiu")}>
            <BasicRecordSelect id="p-ciiu" tableCode="Ciiu" label={t("parties.fields.ciiu")}
              value={v.actividadEconomicaCiiuCode} onChange={(c) => set({ actividadEconomicaCiiuCode: c })} disabled={disabled} />
          </Field>
        </FormGrid>
      )}
      </div>
    </div>
  );
}
