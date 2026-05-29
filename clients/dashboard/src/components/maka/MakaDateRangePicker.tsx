/**
 * MakaDateRangePicker — quick-preset chips + Syncfusion DateRangePicker.
 *
 * Layout (mirrors the audit page's range chips):
 *   [ 24h ] [ 7d ] [ 30d ] | [ 📅 ]
 *
 * - Clicking 24h / 7d / 30d selects a window from now backwards and emits it.
 *   Clicking the active preset again clears the filter.
 * - Clicking the calendar icon opens the Syncfusion range popup directly with
 *   the two month calendars — the picker's own input + button are hidden, so
 *   the icon IS the only trigger.
 * - Theme-aware (CSS tokens, light/dark), i18n + localization aware
 *   (date format + locale + popup labels come from the tenant config).
 *
 * Controlled: pass `value` + `onChange`. Setting `value` to null resets the
 * chip/calendar highlight (e.g. when the page clears all filters).
 */
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { CalendarDays } from "lucide-react";
import { L10n } from "@syncfusion/ej2-base";
import { DateRangePickerComponent } from "@syncfusion/ej2-react-calendars";
import { useLocalization } from "@/contexts/localization-context";
import { cn } from "@/lib/cn";
import "./maka-daterangepicker.css";

// ── Popup label localization (Apply / Cancel / labels / presets) ──────────────
// Registered under the SAME locale strings the component passes to `locale`
// (es-CO / en-US) — otherwise Syncfusion falls back to English.
const DRP_EN = {
  daterangepicker: {
    placeholder: "Choose a date range",
    startLabel: "Start date",
    endLabel: "End date",
    applyText: "Apply",
    cancelText: "Cancel",
    selectedDays: "Selected days",
    days: "days",
    customRange: "Custom range",
  },
  calendar: { today: "Today" },
};
const DRP_ES = {
  daterangepicker: {
    placeholder: "Elige un rango de fechas",
    startLabel: "Fecha inicial",
    endLabel: "Fecha final",
    applyText: "Aplicar",
    cancelText: "Cancelar",
    selectedDays: "Días seleccionados",
    days: "días",
    customRange: "Rango personalizado",
  },
  calendar: { today: "Hoy" },
};
L10n.load({ en: DRP_EN, "en-US": DRP_EN, es: DRP_ES, "es-CO": DRP_ES });

export interface MakaDateRange {
  start: Date;
  end: Date;
}

export type MakaRangePreset = "24h" | "7d" | "30d" | "90d";

const PRESET_MS: Record<MakaRangePreset, number> = {
  "24h": 24 * 60 * 60 * 1000,
  "7d": 7 * 24 * 60 * 60 * 1000,
  "30d": 30 * 24 * 60 * 60 * 1000,
  "90d": 90 * 24 * 60 * 60 * 1000,
};

export interface MakaDateRangePickerProps {
  /** Current range, or null when no filter is applied. */
  value: MakaDateRange | null;
  /** Fires with the new range, or null when cleared. */
  onChange: (range: MakaDateRange | null) => void;
  /** Quick presets to show. Default ["24h", "7d", "30d"]. */
  presets?: MakaRangePreset[];
  /** Optional leading label. */
  label?: string;
  className?: string;
}

export function MakaDateRangePicker({
  value,
  onChange,
  presets = ["24h", "7d", "30d"],
  label,
  className,
}: MakaDateRangePickerProps) {
  const { t } = useTranslation("common");
  const { config } = useLocalization();
  const pickerRef = useRef<DateRangePickerComponent>(null);

  const sfFormat =
    config.dateFormat === "MM/DD/YYYY" ? "MM/dd/yyyy"
    : config.dateFormat === "YYYY-MM-DD" ? "yyyy-MM-dd"
    : "dd/MM/yyyy";
  const sfLocale = config.language === "es" ? "es-CO" : "en-US";

  const [activePreset, setActivePreset] = useState<MakaRangePreset | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);

  // When the parent clears the value, drop any local highlight.
  useEffect(() => {
    if (!value) {
      setActivePreset(null);
    }
  }, [value]);

  const isCustom = !!value && activePreset === null;

  const presetLabel: Record<MakaRangePreset, string> = useMemo(
    () => ({
      "24h": t("dateRange.last24h"),
      "7d": t("dateRange.last7d"),
      "30d": t("dateRange.last30d"),
      "90d": t("dateRange.last90d"),
    }),
    [t],
  );

  const choosePreset = (key: MakaRangePreset) => {
    if (activePreset === key) {
      setActivePreset(null);
      onChange(null);
      return;
    }
    setActivePreset(key);
    pickerRef.current?.hide();
    const end = new Date();
    const start = new Date(end.getTime() - PRESET_MS[key]);
    onChange({ start, end });
  };

  const toggleCustom = () => {
    const picker = pickerRef.current;
    if (!picker) return;
    if (pickerOpen) picker.hide();
    else picker.show();
  };

  const onPickerChange = (e: { startDate?: Date; endDate?: Date }) => {
    if (e.startDate && e.endDate) {
      setActivePreset(null);
      onChange({ start: e.startDate, end: e.endDate });
    } else {
      onChange(null);
    }
  };

  return (
    <div className={cn("inline-flex flex-col gap-1.5", className)}>
      {label && (
        <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {label}
        </span>
      )}

      <div className="inline-flex items-center gap-1 self-start rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-1">
        {presets.map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => choosePreset(key)}
            className={cn(
              "rounded-md px-2.5 py-1 text-[12px] font-medium transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              activePreset === key
                ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            )}
          >
            {presetLabel[key]}
          </button>
        ))}

        <span aria-hidden className="mx-0.5 h-4 w-px bg-[var(--color-border)]" />

        <button
          type="button"
          aria-label={t("dateRange.custom")}
          title={t("dateRange.custom")}
          onClick={toggleCustom}
          className={cn(
            "grid size-7 place-items-center rounded-md transition-colors",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            isCustom || pickerOpen
              ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
              : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          )}
        >
          <CalendarDays className="size-4" />
        </button>

        {/* Hidden anchor — the Syncfusion input/button are visually removed;
            the icon above opens this picker's two-calendar popup directly. */}
        <span className="maka-drp-anchor" aria-hidden>
          <DateRangePickerComponent
            ref={pickerRef}
            locale={sfLocale}
            format={sfFormat}
            placeholder={t("dateRange.placeholder")}
            startDate={isCustom ? value?.start : undefined}
            endDate={isCustom ? value?.end : undefined}
            change={onPickerChange}
            open={() => setPickerOpen(true)}
            close={() => setPickerOpen(false)}
          />
        </span>
      </div>
    </div>
  );
}
