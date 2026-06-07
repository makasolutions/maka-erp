/**
 * MakaDateTimeRangePicker — start/end date+time range (Syncfusion DateTimePicker × 2).
 *
 * Use when a range needs a time-of-day (e.g. campaign validity). Unlike the
 * date-only MakaDateRangePicker, each end is a full DateTimePicker so users pick
 * the exact start/end instant. Controlled via `value` ({ start, end } | null).
 */
import { useEffect } from "react";
import { DateTimePickerComponent } from "@syncfusion/ej2-react-calendars";
import { loadCldr, L10n } from "@syncfusion/ej2-base";
import "./maka-datetime.css";

import gregorian from "cldr-data/main/es-CO/ca-gregorian.json";
import timeZoneNames from "cldr-data/main/es-CO/timeZoneNames.json";
import numbers from "cldr-data/main/es-CO/numbers.json";
import numberingSystems from "cldr-data/supplemental/numberingSystems.json";

let loaded = false;
function ensure() {
  if (loaded) return;
  loadCldr(numberingSystems, gregorian, numbers, timeZoneNames);
  L10n.load({ "es-CO": { datetimepicker: { placeholder: "Elige fecha y hora" } } });
  loaded = true;
}

export interface MakaDateTimeRange {
  start: Date;
  end: Date;
}

export interface MakaDateTimeRangePickerProps {
  value: MakaDateTimeRange | null;
  onChange: (value: MakaDateTimeRange | null) => void;
  startLabel?: string;
  endLabel?: string;
}

export function MakaDateTimeRangePicker({ value, onChange, startLabel, endLabel }: MakaDateTimeRangePickerProps) {
  useEffect(() => { ensure(); }, []);

  const setStart = (d: Date | null) => {
    if (!d) { if (value) onChange(null); return; }
    const end = value && value.end > d ? value.end : new Date(d.getTime() + 60 * 60 * 1000);
    onChange({ start: d, end });
  };
  const setEnd = (d: Date | null) => {
    if (!d || !value) return;
    onChange({ start: value.start, end: d });
  };

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
      <div className="min-w-0 flex-1">
        {startLabel && <span className="mb-0.5 block text-[10px] uppercase tracking-wide text-[var(--color-muted-foreground)]">{startLabel}</span>}
        <DateTimePickerComponent
          locale="es-CO"
          value={value?.start}
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          change={(e: any) => setStart(e?.value ?? null)}
          format="dd/MM/yyyy hh:mm a"
          cssClass="maka-datetime"
        />
      </div>
      <span aria-hidden className="hidden shrink-0 text-[var(--color-muted-foreground)] sm:inline">→</span>
      <div className="min-w-0 flex-1">
        {endLabel && <span className="mb-0.5 block text-[10px] uppercase tracking-wide text-[var(--color-muted-foreground)]">{endLabel}</span>}
        <DateTimePickerComponent
          locale="es-CO"
          value={value?.end}
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          change={(e: any) => setEnd(e?.value ?? null)}
          min={value?.start}
          format="dd/MM/yyyy hh:mm a"
          cssClass="maka-datetime"
        />
      </div>
    </div>
  );
}
