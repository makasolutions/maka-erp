import { useTranslation } from "react-i18next";
import { MapPin, Plus, Star, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Combobox } from "@/components/list";
import { useColombiaGeo } from "./use-colombia-geo";
import type { PartyAddress } from "@/api/parties";

export interface AddressEditorProps {
  value: PartyAddress[];
  onChange: (next: PartyAddress[]) => void;
  disabled?: boolean;
}

export function AddressEditor({ value, onChange, disabled }: AddressEditorProps) {
  const { t } = useTranslation("crm");
  const { departments, citiesOf } = useColombiaGeo();

  const update = (i: number, patch: Partial<PartyAddress>) =>
    onChange(value.map((a, idx) => (idx === i ? { ...a, ...patch } : a)));
  const add = () =>
    onChange([...value, { country: "Colombia", isPrimary: value.length === 0, department: null, city: null, line: "" }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));
  const makePrimary = (i: number) => onChange(value.map((a, idx) => ({ ...a, isPrimary: idx === i })));

  return (
    <div className="space-y-3">
      {value.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.address.empty")}</p>}
      {value.map((a, i) => (
        <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
              <MapPin className="size-3.5" />{a.label?.trim() || `${t("parties.address.singular")} ${i + 1}`}
            </span>
            <div className="flex items-center gap-1">
              <button type="button" disabled={disabled} onClick={() => makePrimary(i)} aria-label={t("parties.address.makePrimary")}
                className={`rounded p-1 ${a.isPrimary ? "text-[var(--color-warning)]" : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"}`}>
                <Star className="size-4" fill={a.isPrimary ? "currentColor" : "none"} />
              </button>
              <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
                className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
            </div>
          </div>
          <div className="grid gap-2 sm:grid-cols-3">
            <Combobox id={`addr-dep-${i}`} label={t("parties.address.department")} value={a.department ?? null}
              onChange={(v) => update(i, { department: v, city: null })} options={departments.map((d) => ({ value: d, label: d }))}
              searchable clearable placeholder={t("parties.address.department")} disabled={disabled} />
            <Combobox id={`addr-city-${i}`} label={t("parties.address.city")} value={a.city ?? null}
              onChange={(v) => update(i, { city: v })} options={citiesOf(a.department).map((c) => ({ value: c, label: c }))}
              searchable clearable placeholder={t("parties.address.city")} disabled={disabled || !a.department} />
            <Input value={a.label ?? ""} disabled={disabled} placeholder={t("parties.address.label")}
              onChange={(e) => update(i, { label: e.target.value })} />
          </div>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
            <Input value={a.line ?? ""} disabled={disabled} placeholder={t("parties.address.line")}
              onChange={(e) => update(i, { line: e.target.value })} />
            <Input value={a.reference ?? ""} disabled={disabled} placeholder={t("parties.address.reference")}
              onChange={(e) => update(i, { reference: e.target.value })} />
          </div>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
            <Input type="number" step="0.0000001" value={a.latitude ?? ""} disabled={disabled} placeholder={t("parties.address.lat")}
              onChange={(e) => update(i, { latitude: e.target.value === "" ? null : Number(e.target.value) })} />
            <Input type="number" step="0.0000001" value={a.longitude ?? ""} disabled={disabled} placeholder={t("parties.address.lng")}
              onChange={(e) => update(i, { longitude: e.target.value === "" ? null : Number(e.target.value) })} />
          </div>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.address.add")}</Button>
      )}
    </div>
  );
}
