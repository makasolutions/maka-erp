import { useTranslation } from "react-i18next";
import { MapPin, Plus, Star, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Field, FormGrid } from "@/components/list";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { CityPicker } from "./CityPicker";
import type { PartyAddress } from "@/api/parties";

export interface AddressEditorProps {
  value: PartyAddress[];
  onChange: (next: PartyAddress[]) => void;
  disabled?: boolean;
  /** Client validation map keyed `addresses.{i}.{field}`. */
  errors?: Record<string, string>;
}

export function AddressEditor({ value, onChange, disabled, errors }: AddressEditorProps) {
  const { t } = useTranslation("crm");
  const err = (i: number, field: string): string | undefined => errors?.[`addresses.${i}.${field}`];

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
              <MapPin className="size-3.5" />{`${t("parties.address.singular")} ${i + 1}`}
            </span>
            <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
              className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
          </div>
          <FormGrid>
            {/* Row 1: Etiqueta · Departamento · Ciudad (cascada DIVIPOLA) */}
            <Field id={`addr-label-${i}`} span={4} label={t("parties.address.label")}>
              <BasicRecordSelect id={`addr-label-${i}`} tableCode="AddressLabel" label={t("parties.address.label")}
                value={a.labelCode ?? null} onChange={(v) => update(i, { labelCode: v })} disabled={disabled} />
            </Field>
            <CityPicker idPrefix={`addr-geo-${i}`} disabled={disabled} cityError={err(i, "city")}
              value={{ departmentCode: a.departmentCode, municipalityCode: a.municipalityCode, department: a.department, city: a.city }}
              onChange={(patch) => update(i, patch)} />
            {/* Row 2: Dirección · Barrio · Referencia */}
            <Field id={`addr-line-${i}`} span={4} label={t("parties.address.line")} required
              error={err(i, "line")} hint={err(i, "line") ? undefined : t("parties.address.lineHint")}>
              <Input id={`addr-line-${i}`} value={a.line ?? ""} disabled={disabled} placeholder="CL 100 # 13-21"
                onChange={(e) => update(i, { line: e.target.value })} />
              {a.normalizedLine && (
                <p className="mt-1 flex items-center gap-1 text-[11px] text-[var(--color-muted-foreground)]">
                  <span className="font-semibold uppercase tracking-wide">{t("parties.address.normalized")}:</span>
                  <code className="font-mono text-[var(--color-foreground)]">{a.normalizedLine}</code>
                </p>
              )}
            </Field>
            <Field id={`addr-barrio-${i}`} span={4} label={t("parties.address.barrio")}>
              <Input id={`addr-barrio-${i}`} value={a.barrio ?? ""} disabled={disabled}
                onChange={(e) => update(i, { barrio: e.target.value })} />
            </Field>
            <Field id={`addr-ref-${i}`} span={4} label={t("parties.address.reference")}>
              <Input id={`addr-ref-${i}`} value={a.reference ?? ""} disabled={disabled}
                onChange={(e) => update(i, { reference: e.target.value })} />
            </Field>
            {/* Row 3: Latitud · Longitud · Predeterminada */}
            <Field id={`addr-lat-${i}`} span={4} label={t("parties.address.lat")} error={err(i, "latitude") ?? err(i, "geo")}>
              <Input id={`addr-lat-${i}`} type="number" step="0.0000001" min={-90} max={90} value={a.latitude ?? ""} disabled={disabled}
                onChange={(e) => update(i, { latitude: e.target.value === "" ? null : Number(e.target.value) })} />
            </Field>
            <Field id={`addr-lng-${i}`} span={4} label={t("parties.address.lng")} error={err(i, "longitude")}>
              <Input id={`addr-lng-${i}`} type="number" step="0.0000001" min={-180} max={180} value={a.longitude ?? ""} disabled={disabled}
                onChange={(e) => update(i, { longitude: e.target.value === "" ? null : Number(e.target.value) })} />
            </Field>
            <Field id={`addr-primary-${i}`} span={4} label={t("parties.address.primary")}>
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={a.isPrimary} disabled={disabled} onCheckedChange={() => makePrimary(i)} />
                <Star className="size-3.5" fill={a.isPrimary ? "currentColor" : "none"} />{t("parties.address.primary")}
              </label>
            </Field>
          </FormGrid>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.address.add")}</Button>
      )}
    </div>
  );
}
