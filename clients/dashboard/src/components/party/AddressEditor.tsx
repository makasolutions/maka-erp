import { useTranslation } from "react-i18next";
import { MapPin, Plus, Star, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Combobox } from "@/components/list";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { useColombiaGeo } from "./use-colombia-geo";
import type { PartyAddress } from "@/api/parties";

export interface AddressEditorProps {
  value: PartyAddress[];
  onChange: (next: PartyAddress[]) => void;
  disabled?: boolean;
}

export function AddressEditor({ value, onChange, disabled }: AddressEditorProps) {
  const { t } = useTranslation("crm");
  const { allCities, deptOfCity } = useColombiaGeo();

  const update = (i: number, patch: Partial<PartyAddress>) =>
    onChange(value.map((a, idx) => (idx === i ? { ...a, ...patch } : a)));
  const add = () =>
    onChange([...value, { country: "Colombia", isPrimary: value.length === 0, department: null, city: null, line: "" }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));
  const makePrimary = (i: number) => onChange(value.map((a, idx) => ({ ...a, isPrimary: idx === i })));
  // Selecting a city auto-fills its department (city-first cascade).
  const setCity = (i: number, city: string | null) => update(i, { city, department: deptOfCity(city) });

  return (
    <div className="space-y-3">
      {value.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.address.empty")}</p>}
      {value.map((a, i) => (
        <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
              <MapPin className="size-3.5" />{`${t("parties.address.singular")} ${i + 1}`}
            </span>
            <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
              className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
          </div>
          {/* Row 1: Etiqueta · Ciudad · Departamento */}
          <div className="grid gap-2 sm:grid-cols-3">
            <BasicRecordSelect id={`addr-label-${i}`} tableCode="AddressLabel" label={t("parties.address.label")}
              value={a.labelCode ?? null} onChange={(v) => update(i, { labelCode: v })} disabled={disabled} />
            <Combobox id={`addr-city-${i}`} label={t("parties.address.city")} value={a.city ?? null}
              onChange={(v) => setCity(i, v)} options={allCities.map((c) => ({ value: c, label: c }))}
              searchable clearable placeholder={t("parties.address.city")} disabled={disabled} />
            <Input value={a.department ?? ""} disabled placeholder={t("parties.address.department")} aria-label={t("parties.address.department")} />
          </div>
          {/* Row 2: Dirección · Barrio · Referencia */}
          <div className="mt-2 grid gap-2 sm:grid-cols-3">
            <Input value={a.line ?? ""} disabled={disabled} placeholder={t("parties.address.line")}
              onChange={(e) => update(i, { line: e.target.value })} />
            <Input value={a.barrio ?? ""} disabled={disabled} placeholder={t("parties.address.barrio")}
              onChange={(e) => update(i, { barrio: e.target.value })} />
            <Input value={a.reference ?? ""} disabled={disabled} placeholder={t("parties.address.reference")}
              onChange={(e) => update(i, { reference: e.target.value })} />
          </div>
          {/* Row 3: Lat · Long · Predeterminada */}
          <div className="mt-2 grid items-center gap-2 sm:grid-cols-3">
            <Input type="number" step="0.0000001" value={a.latitude ?? ""} disabled={disabled} placeholder={t("parties.address.lat")}
              onChange={(e) => update(i, { latitude: e.target.value === "" ? null : Number(e.target.value) })} />
            <Input type="number" step="0.0000001" value={a.longitude ?? ""} disabled={disabled} placeholder={t("parties.address.lng")}
              onChange={(e) => update(i, { longitude: e.target.value === "" ? null : Number(e.target.value) })} />
            <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
              <Switch checked={a.isPrimary} disabled={disabled} onCheckedChange={() => makePrimary(i)} />
              <Star className="size-3.5" fill={a.isPrimary ? "currentColor" : "none"} />{t("parties.address.primary")}
            </label>
          </div>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.address.add")}</Button>
      )}
    </div>
  );
}
