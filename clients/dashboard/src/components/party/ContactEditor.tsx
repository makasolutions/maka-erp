import { useTranslation } from "react-i18next";
import { Plus, Trash2, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import type { PartyContact } from "@/api/parties";

export interface ContactEditorProps {
  value: PartyContact[];
  onChange: (next: PartyContact[]) => void;
  disabled?: boolean;
}

export function ContactEditor({ value, onChange, disabled }: ContactEditorProps) {
  const { t } = useTranslation("crm");
  const update = (i: number, patch: Partial<PartyContact>) =>
    onChange(value.map((c, idx) => (idx === i ? { ...c, ...patch } : c)));
  const add = () => onChange([...value, { reference: "", isCommercial: false }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      {value.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.contact.empty")}</p>}
      {value.map((c, i) => {
        const missingChannel = !(c.email ?? "").trim() || !(c.cell ?? "").trim();
        return (
          <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
            <div className="mb-2 flex items-center justify-between">
              <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
                <UserRound className="size-3.5" />{`${c.firstName ?? ""} ${c.lastName ?? ""}`.trim() || `${t("parties.contact.singular")} ${i + 1}`}
              </span>
              <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
                className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
            </div>
            <div className="grid gap-2 sm:grid-cols-3">
              <BasicRecordSelect id={`ct-type-${i}`} tableCode="ContactType" label={t("parties.contact.type")}
                value={c.contactTypeCode ?? null} onChange={(v) => update(i, { contactTypeCode: v })} disabled={disabled} />
              <BasicRecordSelect id={`ct-area-${i}`} tableCode="ContactArea" label={t("parties.contact.area")}
                value={c.areaCode ?? null} onChange={(v) => update(i, { areaCode: v })} disabled={disabled} />
              <Input value={c.reference} disabled={disabled} placeholder={t("parties.contact.reference")}
                onChange={(e) => update(i, { reference: e.target.value })} />
            </div>
            <div className="mt-2 grid gap-2 sm:grid-cols-3">
              <BasicRecordSelect id={`ct-idtype-${i}`} tableCode="IdentificationType" label={t("parties.contact.idType")}
                value={c.identificationTypeCode ?? null} onChange={(v) => update(i, { identificationTypeCode: v })} disabled={disabled} />
              <Input value={c.identificationNumber ?? ""} disabled={disabled} placeholder={t("parties.contact.idNumber")} className="font-mono"
                onChange={(e) => update(i, { identificationNumber: e.target.value })} />
              <BasicRecordSelect id={`ct-pos-${i}`} tableCode="Position" label={t("parties.contact.position")}
                value={c.positionCode ?? null} onChange={(v) => update(i, { positionCode: v })} disabled={disabled} />
            </div>
            <div className="mt-2 grid gap-2 sm:grid-cols-3">
              <Input value={c.firstName ?? ""} disabled={disabled} placeholder={t("parties.contact.firstName")}
                onChange={(e) => update(i, { firstName: e.target.value })} />
              <Input value={c.lastName ?? ""} disabled={disabled} placeholder={t("parties.contact.lastName")}
                onChange={(e) => update(i, { lastName: e.target.value })} />
              <BasicRecordSelect id={`ct-prof-${i}`} tableCode="Profession" label={t("parties.contact.profession")}
                value={c.professionCode ?? null} onChange={(v) => update(i, { professionCode: v })} disabled={disabled} />
            </div>
            <div className="mt-2 grid gap-2 sm:grid-cols-3">
              <Input type="date" value={c.birthDate ?? ""} disabled={disabled} aria-label={t("parties.contact.birthDate")}
                onChange={(e) => update(i, { birthDate: e.target.value })} />
              <BasicRecordSelect id={`ct-gen-${i}`} tableCode="Gender" label={t("parties.contact.gender")}
                value={c.genderCode ?? null} onChange={(v) => update(i, { genderCode: v })} disabled={disabled} />
              <BasicRecordSelect id={`ct-civ-${i}`} tableCode="MaritalStatus" label={t("parties.contact.maritalStatus")}
                value={c.maritalStatusCode ?? null} onChange={(v) => update(i, { maritalStatusCode: v })} disabled={disabled} />
            </div>
            <div className="mt-2 grid items-center gap-2 sm:grid-cols-3">
              <Input value={c.email ?? ""} disabled={disabled} placeholder={`${t("parties.contact.email")} *`}
                aria-invalid={!(c.email ?? "").trim()} onChange={(e) => update(i, { email: e.target.value })} />
              <Input value={c.cell ?? ""} disabled={disabled} placeholder={`${t("parties.contact.cell")} *`}
                aria-invalid={!(c.cell ?? "").trim()} onChange={(e) => update(i, { cell: e.target.value })} />
              <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={c.isCommercial} disabled={disabled} onCheckedChange={(v) => update(i, { isCommercial: v })} />
                {t("parties.contact.isCommercial")}
              </label>
            </div>
            {missingChannel && <p className="mt-1.5 text-[11.5px] text-[var(--color-destructive)]">{t("parties.contact.channelRequired")}</p>}
          </div>
        );
      })}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.contact.add")}</Button>
      )}
    </div>
  );
}
