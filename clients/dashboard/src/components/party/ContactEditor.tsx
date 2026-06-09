import { useTranslation } from "react-i18next";
import { Plus, Trash2, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Field, FormGrid } from "@/components/list";
import { MakaDatePicker } from "@/components/maka";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { isNumericIdType } from "@/lib/validation/predicates";
import type { PartyContact } from "@/api/parties";

export interface ContactEditorProps {
  value: PartyContact[];
  onChange: (next: PartyContact[]) => void;
  disabled?: boolean;
  /** Client validation map keyed `contacts.{i}.{field}`. */
  errors?: Record<string, string>;
}

const TODAY = new Date();

export function ContactEditor({ value, onChange, disabled, errors }: ContactEditorProps) {
  const { t } = useTranslation("crm");
  const err = (i: number, field: string): string | undefined => errors?.[`contacts.${i}.${field}`];

  const update = (i: number, patch: Partial<PartyContact>) =>
    onChange(value.map((c, idx) => (idx === i ? { ...c, ...patch } : c)));
  const add = () => onChange([...value, { reference: "", isCommercial: false }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      {value.length === 0 && (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.contact.empty")}</p>
      )}
      {value.map((c, i) => {
        const numericId = isNumericIdType(c.identificationTypeCode);
        return (
          <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
            <div className="mb-2 flex items-center justify-between">
              <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
                <UserRound className="size-3.5" />
                {`${c.firstName ?? ""} ${c.lastName ?? ""}`.trim() || `${t("parties.contact.singular")} ${i + 1}`}
              </span>
              <button type="button" disabled={disabled} onClick={() => remove(i)}
                aria-label={t("common:actions.delete", "Eliminar")}
                className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
                <Trash2 className="size-4" />
              </button>
            </div>

            <FormGrid>
              {/* Clasificación */}
              <Field id={`ct-type-${i}`} span={4} label={t("parties.contact.type")}>
                <BasicRecordSelect id={`ct-type-${i}`} tableCode="ContactType" label={t("parties.contact.type")}
                  value={c.contactTypeCode ?? null} onChange={(v) => update(i, { contactTypeCode: v })} disabled={disabled} />
              </Field>
              <Field id={`ct-area-${i}`} span={4} label={t("parties.contact.area")}>
                <BasicRecordSelect id={`ct-area-${i}`} tableCode="ContactArea" label={t("parties.contact.area")}
                  value={c.areaCode ?? null} onChange={(v) => update(i, { areaCode: v })} disabled={disabled} />
              </Field>
              <Field id={`ct-ref-${i}`} span={4} label={t("parties.contact.reference")}>
                <Input id={`ct-ref-${i}`} value={c.reference} disabled={disabled} maxLength={128}
                  onChange={(e) => update(i, { reference: e.target.value })} />
              </Field>

              {/* Identidad */}
              <Field id={`ct-idtype-${i}`} span={4} label={t("parties.contact.idType")}>
                <BasicRecordSelect id={`ct-idtype-${i}`} tableCode="IdentificationType" label={t("parties.contact.idType")}
                  value={c.identificationTypeCode ?? null} onChange={(v) => update(i, { identificationTypeCode: v })} disabled={disabled} />
              </Field>
              <Field id={`ct-idnum-${i}`} span={4} label={t("parties.contact.idNumber")} error={err(i, "identificationNumber")}>
                <Input id={`ct-idnum-${i}`} value={c.identificationNumber ?? ""} disabled={disabled} className="font-mono" maxLength={20}
                  inputMode={numericId ? "numeric" : "text"}
                  onChange={(e) => update(i, { identificationNumber: numericId ? e.target.value.replace(/\D/g, "") : e.target.value })} />
              </Field>
              <Field id={`ct-pos-${i}`} span={4} label={t("parties.contact.position")}>
                <BasicRecordSelect id={`ct-pos-${i}`} tableCode="Position" label={t("parties.contact.position")}
                  value={c.positionCode ?? null} onChange={(v) => update(i, { positionCode: v })} disabled={disabled} />
              </Field>

              {/* Nombre */}
              <Field id={`ct-first-${i}`} span={4} label={t("parties.contact.firstName")} error={err(i, "firstName")}>
                <Input id={`ct-first-${i}`} value={c.firstName ?? ""} disabled={disabled} maxLength={50}
                  onChange={(e) => update(i, { firstName: e.target.value })} />
              </Field>
              <Field id={`ct-last-${i}`} span={4} label={t("parties.contact.lastName")} error={err(i, "lastName")}>
                <Input id={`ct-last-${i}`} value={c.lastName ?? ""} disabled={disabled} maxLength={50}
                  onChange={(e) => update(i, { lastName: e.target.value })} />
              </Field>
              <Field id={`ct-prof-${i}`} span={4} label={t("parties.contact.profession")}>
                <BasicRecordSelect id={`ct-prof-${i}`} tableCode="Profession" label={t("parties.contact.profession")}
                  value={c.professionCode ?? null} onChange={(v) => update(i, { professionCode: v })} disabled={disabled} />
              </Field>

              {/* Demográficos */}
              <Field id={`ct-birth-${i}`} span={4} label={t("parties.contact.birthDate")} error={err(i, "birthDate")}>
                <MakaDatePicker id={`ct-birth-${i}`} value={c.birthDate ?? null} disabled={disabled} max={TODAY}
                  onChange={(iso) => update(i, { birthDate: iso })} />
              </Field>
              <Field id={`ct-gen-${i}`} span={4} label={t("parties.contact.gender")}>
                <BasicRecordSelect id={`ct-gen-${i}`} tableCode="Gender" label={t("parties.contact.gender")}
                  value={c.genderCode ?? null} onChange={(v) => update(i, { genderCode: v })} disabled={disabled} />
              </Field>
              <Field id={`ct-civ-${i}`} span={4} label={t("parties.contact.maritalStatus")}>
                <BasicRecordSelect id={`ct-civ-${i}`} tableCode="MaritalStatus" label={t("parties.contact.maritalStatus")}
                  value={c.maritalStatusCode ?? null} onChange={(v) => update(i, { maritalStatusCode: v })} disabled={disabled} />
              </Field>

              {/* Canales requeridos */}
              <Field id={`ct-email-${i}`} span={4} label={t("parties.contact.email")} required error={err(i, "email")}>
                <Input id={`ct-email-${i}`} type="email" value={c.email ?? ""} disabled={disabled} maxLength={254}
                  onChange={(e) => update(i, { email: e.target.value })} />
              </Field>
              <Field id={`ct-cell-${i}`} span={4} label={t("parties.contact.cell")} required error={err(i, "cell")}>
                <Input id={`ct-cell-${i}`} value={c.cell ?? ""} disabled={disabled} inputMode="tel" maxLength={20}
                  onChange={(e) => update(i, { cell: e.target.value })} />
              </Field>
              <Field id={`ct-comm-${i}`} span={4} label={t("parties.contact.isCommercial")}>
                <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={c.isCommercial} disabled={disabled} onCheckedChange={(v) => update(i, { isCommercial: v })} />
                  {t("parties.contact.isCommercial")}
                </label>
              </Field>
            </FormGrid>
          </div>
        );
      })}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" className="w-full sm:w-auto" onClick={add}>
          <Plus className="size-4" />{t("parties.contact.add")}
        </Button>
      )}
    </div>
  );
}
