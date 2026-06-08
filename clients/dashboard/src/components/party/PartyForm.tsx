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
import {
  rolesToApi, type LifecycleStage, type PartyAddress, type PartyChannel, type PartyContact,
  type PartyDetailDto, type PartyKind, type PartyRoles, type PartyStatus, type PartyWriteInput,
} from "@/api/parties";

export type PartyFormValue = {
  identificationTypeCode: string | null;
  identificationNumber: string;
  verificationDigit: number | null;
  kind: PartyKind;
  legalName: string;
  tradeName: string;
  customer: boolean;
  supplier: boolean;
  email: string;
  website: string;
  taxRegimeCode: string | null;
  fiscalResponsibilities: string;
  status: PartyStatus;
  stage: LifecycleStage;
  leadScore: number;
  sourceCode: string | null;
  marketingType: string;
  birthDate: string;
  genderCode: string | null;
  maritalStatusCode: string | null;
  creditLimit: number | null;
  creditCurrency: string;
  notes: string;
  addresses: PartyAddress[];
  contacts: PartyContact[];
  channels: PartyChannel[];
};

export function emptyPartyForm(): PartyFormValue {
  return {
    identificationTypeCode: null, identificationNumber: "", verificationDigit: null, kind: "Juridica",
    legalName: "", tradeName: "", customer: true, supplier: false, email: "", website: "",
    taxRegimeCode: null, fiscalResponsibilities: "", status: "Active", stage: "Lead", leadScore: 0,
    sourceCode: null, marketingType: "", birthDate: "", genderCode: null, maritalStatusCode: null,
    creditLimit: null, creditCurrency: "", notes: "", addresses: [], contacts: [], channels: [],
  };
}

function rolesHas(roles: PartyRoles, r: string) { return roles.includes(r); }

export function partyFormFromDetail(d: PartyDetailDto): PartyFormValue {
  return {
    identificationTypeCode: d.identificationTypeCode, identificationNumber: d.identificationNumber,
    verificationDigit: d.verificationDigit ?? null, kind: d.kind, legalName: d.legalName, tradeName: d.tradeName ?? "",
    customer: rolesHas(d.roles, "Customer"), supplier: rolesHas(d.roles, "Supplier"),
    email: d.email ?? "", website: d.website ?? "", taxRegimeCode: d.taxRegimeCode ?? null,
    fiscalResponsibilities: d.fiscalResponsibilities ?? "", status: d.status, stage: d.stage, leadScore: d.leadScore,
    sourceCode: d.sourceCode ?? null, marketingType: d.marketingType ?? "", birthDate: d.birthDate ?? "",
    genderCode: d.genderCode ?? null, maritalStatusCode: d.maritalStatusCode ?? null,
    creditLimit: d.creditLimit ?? null, creditCurrency: d.creditCurrency ?? "", notes: d.notes ?? "",
    addresses: d.addresses, contacts: d.contacts, channels: d.channels,
  };
}

export function partyFormToInput(v: PartyFormValue): PartyWriteInput {
  return {
    identificationTypeCode: v.identificationTypeCode ?? "", identificationNumber: v.identificationNumber.trim(),
    verificationDigit: v.verificationDigit, kind: v.kind, legalName: v.legalName.trim(),
    roles: rolesToApi(v.customer, v.supplier), tradeName: v.tradeName.trim() || null, email: v.email.trim() || null,
    website: v.website.trim() || null, taxRegimeCode: v.taxRegimeCode, fiscalResponsibilities: v.fiscalResponsibilities.trim() || null,
    status: v.status, stage: v.stage, leadScore: v.leadScore, sourceCode: v.sourceCode, marketingType: v.marketingType.trim() || null,
    birthDate: v.birthDate || null, genderCode: v.genderCode, maritalStatusCode: v.maritalStatusCode,
    creditLimit: v.creditLimit, creditCurrency: v.creditCurrency.trim() || null, notes: v.notes.trim() || null, branchId: null,
    addresses: v.addresses, contacts: v.contacts, channels: v.channels, team: [],
  };
}

export interface PartyFormProps {
  value: PartyFormValue;
  onChange: (next: PartyFormValue) => void;
  isCreate: boolean;
  disabled?: boolean;
}

export function PartyForm({ value: v, onChange, isCreate, disabled }: PartyFormProps) {
  const { t } = useTranslation("crm");
  const set = (patch: Partial<PartyFormValue>) => onChange({ ...v, ...patch });

  const statusOpts: PartyStatus[] = ["Active", "Inactive", "Prospect"];
  const stageOpts: LifecycleStage[] = ["Lead", "Mql", "Sql", "Opportunity", "Customer", "Inactive"];
  const isNit = (v.identificationTypeCode ?? "") === "NIT";

  return (
    <div className="space-y-6">
      {/* Identificación */}
      <FormGrid>
        <Field id="p-idtype" span={3} label={t("parties.fields.idType")} required>
          <BasicRecordSelect id="p-idtype" tableCode="IdentificationType" label={t("parties.fields.idType")}
            value={v.identificationTypeCode} onChange={(c) => set({ identificationTypeCode: c })} disabled={disabled || !isCreate} />
        </Field>
        <Field id="p-idnum" span={3} label={t("parties.fields.idNumber")} required>
          <Input id="p-idnum" value={v.identificationNumber} disabled={disabled || !isCreate} className="font-mono"
            onChange={(e) => set({ identificationNumber: e.target.value })} />
        </Field>
        <Field id="p-dv" span={2} label={t("parties.fields.dv")} hint={isNit ? t("parties.fields.dvHint") : undefined}>
          <div className="flex gap-1">
            <Input id="p-dv" type="number" className="font-mono" value={v.verificationDigit ?? ""} disabled={disabled}
              onChange={(e) => set({ verificationDigit: e.target.value === "" ? null : Number(e.target.value) })} />
            {isNit && !disabled && (
              <Button type="button" variant="outline" size="sm" title={t("parties.fields.dvCompute")}
                onClick={() => set({ verificationDigit: nitVerificationDigit(v.identificationNumber) })}>DV</Button>
            )}
          </div>
        </Field>
        <Field id="p-kind" span={4} label={t("parties.fields.kind")} required>
          <Combobox id="p-kind" label={t("parties.fields.kind")} value={v.kind} onChange={(k) => k && set({ kind: k as PartyKind })}
            options={[{ value: "Juridica", label: t("parties.kind.Juridica") }, { value: "Natural", label: t("parties.kind.Natural") }]} disabled={disabled} />
        </Field>
        <Field id="p-legal" span={8} label={t("parties.fields.legalName")} required>
          <Input id="p-legal" value={v.legalName} disabled={disabled} onChange={(e) => set({ legalName: e.target.value })} />
        </Field>
        <Field id="p-trade" span={4} label={t("parties.fields.tradeName")}>
          <Input id="p-trade" value={v.tradeName} disabled={disabled} onChange={(e) => set({ tradeName: e.target.value })} />
        </Field>
        <Field id="p-email" span={6} label={t("parties.fields.email")}>
          <Input id="p-email" type="email" value={v.email} disabled={disabled} onChange={(e) => set({ email: e.target.value })} />
        </Field>
        <Field id="p-web" span={6} label={t("parties.fields.website")}>
          <Input id="p-web" value={v.website} disabled={disabled} onChange={(e) => set({ website: e.target.value })} />
        </Field>
        <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
          <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("parties.fields.roles")}</span>
          <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
            <Switch checked={v.customer} disabled={disabled} onCheckedChange={(c) => set({ customer: c })} />{t("parties.role.customer")}
          </label>
          <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
            <Switch checked={v.supplier} disabled={disabled} onCheckedChange={(c) => set({ supplier: c })} />{t("parties.role.supplier")}
          </label>
        </div>
      </FormGrid>

      {/* Fiscal + CRM */}
      <div className="border-t border-[var(--color-border)] pt-4">
        <FormGrid>
          <Field id="p-regime" span={4} label={t("parties.fields.taxRegime")}>
            <BasicRecordSelect id="p-regime" tableCode="TaxRegime" label={t("parties.fields.taxRegime")}
              value={v.taxRegimeCode} onChange={(c) => set({ taxRegimeCode: c })} disabled={disabled} />
          </Field>
          <Field id="p-fiscal" span={8} label={t("parties.fields.fiscalResp")}>
            <Input id="p-fiscal" value={v.fiscalResponsibilities} disabled={disabled}
              onChange={(e) => set({ fiscalResponsibilities: e.target.value })} />
          </Field>
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
        </FormGrid>
      </div>

      {/* Persona natural + B2B */}
      <div className="border-t border-[var(--color-border)] pt-4">
        <FormGrid>
          {v.kind === "Natural" && (
            <>
              <Field id="p-birth" span={3} label={t("parties.fields.birthDate")}>
                <Input id="p-birth" type="date" value={v.birthDate} disabled={disabled} onChange={(e) => set({ birthDate: e.target.value })} />
              </Field>
              <Field id="p-gender" span={3} label={t("parties.fields.gender")}>
                <BasicRecordSelect id="p-gender" tableCode="Gender" label={t("parties.fields.gender")}
                  value={v.genderCode} onChange={(c) => set({ genderCode: c })} disabled={disabled} />
              </Field>
              <Field id="p-marital" span={3} label={t("parties.fields.maritalStatus")}>
                <BasicRecordSelect id="p-marital" tableCode="MaritalStatus" label={t("parties.fields.maritalStatus")}
                  value={v.maritalStatusCode} onChange={(c) => set({ maritalStatusCode: c })} disabled={disabled} />
              </Field>
            </>
          )}
          <Field id="p-credit" span={3} label={t("parties.fields.creditLimit")}>
            <MakaCurrencyInput id="p-credit" value={v.creditLimit} disabled={disabled} onChange={(n) => set({ creditLimit: n })} />
          </Field>
          <Field id="p-notes" span={12} label={t("parties.fields.notes")}>
            <Textarea id="p-notes" rows={3} value={v.notes} disabled={disabled} onChange={(e) => set({ notes: e.target.value })} />
          </Field>
        </FormGrid>
      </div>

      {/* Direcciones */}
      <div className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("parties.address.title")}</h3>
        <AddressEditor value={v.addresses} onChange={(a) => set({ addresses: a })} disabled={disabled} />
      </div>

      {/* Canales */}
      <div className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("parties.channel.title")}</h3>
        <ChannelEditor value={v.channels} onChange={(c) => set({ channels: c })} disabled={disabled} />
      </div>

      {/* Contactos */}
      <div className="border-t border-[var(--color-border)] pt-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("parties.contact.title")}</h3>
        <ContactEditor value={v.contacts} onChange={(c) => set({ contacts: c })} disabled={disabled} />
      </div>
    </div>
  );
}
