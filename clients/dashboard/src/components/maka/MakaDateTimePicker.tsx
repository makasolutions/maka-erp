/**
 * MakaDateTimePicker — single date+time Syncfusion picker (SfDateTimePicker) wrapper.
 * The hour-aware sibling of MakaDatePicker: same theming (carries the shared `maka-datetime`
 * cssClass → token-driven chrome + 3px radius + themed calendar/time popups) and localization
 * (date/time format + locale from tenant config), but emits/consumes an ISO `yyyy-MM-ddTHH:mm`
 * string so it drops into existing string-date models that need a time component.
 *
 * Controlled: pass `value` (ISO or null) + `onChange`. `min`/`max` clamp the selectable range.
 * Renders a standard input, so `Field`'s error tinting applies. Per house rule §17/§18, this is
 * the ONLY single date+time control — never use a raw `<input type="datetime-local">`.
 */
import { useMemo } from "react";
import { DateTimePickerComponent } from "@syncfusion/ej2-react-calendars";
import { useLocalization } from "@/contexts/localization-context";
import "./maka-datetime.css";

export interface MakaDateTimePickerProps {
  id?: string;
  /** ISO `yyyy-MM-ddTHH:mm` (or any Date-parseable string), or null/empty. */
  value: string | null;
  onChange: (iso: string | null) => void;
  min?: Date;
  max?: Date;
  /** Minute step for the time list. Defaults to 15. */
  step?: number;
  disabled?: boolean;
  placeholder?: string;
  className?: string;
}

const pad = (n: number) => String(n).padStart(2, "0");
/** Date → local ISO `yyyy-MM-ddTHH:mm` (no timezone shift). */
const toIso = (d: Date | null | undefined): string | null =>
  d && !Number.isNaN(d.getTime())
    ? `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
    : null;
/** ISO `yyyy-MM-ddTHH:mm` → local Date (no TZ shift). */
const fromIso = (iso: string | null): Date | undefined => {
  const v = (iso ?? "").trim();
  if (v === "") return undefined;
  const m = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2}))?/.exec(v);
  if (m) {
    return new Date(
      Number(m[1]), Number(m[2]) - 1, Number(m[3]),
      m[4] ? Number(m[4]) : 0, m[5] ? Number(m[5]) : 0,
    );
  }
  const d = new Date(v);
  return Number.isNaN(d.getTime()) ? undefined : d;
};

export function MakaDateTimePicker({
  id, value, onChange, min, max, step = 15, disabled, placeholder, className,
}: MakaDateTimePickerProps) {
  const { config } = useLocalization();

  const datePart =
    config.dateFormat === "MM/DD/YYYY" ? "MM/dd/yyyy"
    : config.dateFormat === "YYYY-MM-DD" ? "yyyy-MM-dd"
    : "dd/MM/yyyy";
  const timePart = config.timeFormat === "24h" ? "HH:mm" : "hh:mm a";
  const sfFormat = `${datePart} ${timePart}`;
  const sfLocale = config.language === "es" ? "es-CO" : "en-US";

  const current = useMemo(() => fromIso(value), [value]);

  return (
    <DateTimePickerComponent
      id={id}
      cssClass={`maka-datetime${className ? ` ${className}` : ""}`}
      locale={sfLocale}
      format={sfFormat}
      value={current}
      min={min}
      max={max}
      step={step}
      enabled={!disabled}
      placeholder={placeholder}
      allowEdit
      showClearButton
      change={(e: { value?: Date | null }) => onChange(toIso(e?.value ?? null))}
    />
  );
}
