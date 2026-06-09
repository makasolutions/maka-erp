/**
 * MakaDatePicker — single-date Syncfusion picker (SfDatePicker) wrapper. Replaces every
 * raw `<input type="date">` per the house rule (§17 / §18). Theme-aware (global bootstrap5
 * theme + tokens), localized (date format + locale from the tenant config), and emits/consumes
 * an ISO `yyyy-MM-dd` string so it drops into the existing string-date data models.
 *
 * Controlled: pass `value` (ISO or null) + `onChange`. `min`/`max` clamp the selectable range
 * (e.g. birth date: max = today). Renders a standard input, so `Field`'s error tinting applies.
 */
import { useMemo } from "react";
import { DatePickerComponent } from "@syncfusion/ej2-react-calendars";
import { useLocalization } from "@/contexts/localization-context";

export interface MakaDatePickerProps {
  id?: string;
  /** ISO `yyyy-MM-dd` (or any Date-parseable string), or null/empty. */
  value: string | null;
  onChange: (iso: string | null) => void;
  min?: Date;
  max?: Date;
  disabled?: boolean;
  placeholder?: string;
  className?: string;
}

const pad = (n: number) => String(n).padStart(2, "0");
/** Date → local ISO `yyyy-MM-dd` (no timezone shift). */
const toIso = (d: Date | null | undefined): string | null =>
  d && !Number.isNaN(d.getTime()) ? `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` : null;
/** ISO `yyyy-MM-dd` → local Date (parsed as local midnight, no TZ shift). */
const fromIso = (iso: string | null): Date | undefined => {
  const v = (iso ?? "").trim();
  if (v === "") return undefined;
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(v);
  if (m) return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
  const d = new Date(v);
  return Number.isNaN(d.getTime()) ? undefined : d;
};

export function MakaDatePicker({
  id, value, onChange, min, max, disabled, placeholder, className,
}: MakaDatePickerProps) {
  const { config } = useLocalization();

  const sfFormat =
    config.dateFormat === "MM/DD/YYYY" ? "MM/dd/yyyy"
    : config.dateFormat === "YYYY-MM-DD" ? "yyyy-MM-dd"
    : "dd/MM/yyyy";
  const sfLocale = config.language === "es" ? "es-CO" : "en-US";

  const current = useMemo(() => fromIso(value), [value]);

  return (
    <DatePickerComponent
      id={id}
      cssClass={className}
      locale={sfLocale}
      format={sfFormat}
      value={current}
      min={min}
      max={max}
      enabled={!disabled}
      placeholder={placeholder}
      allowEdit
      showClearButton
      change={(e: { value?: Date | null }) => onChange(toIso(e?.value ?? null))}
    />
  );
}
