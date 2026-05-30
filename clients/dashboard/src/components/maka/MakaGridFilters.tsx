/**
 * MakaGridFilters — the collapsible Filters / KPIs panel that sits above a
 * MakaGrid. Generic + reusable across pages.
 *
 * Layout:
 *   ┌─ [ Filtros ] [ Indicadores ] ───────────────────────────┐
 *   │  <filters slot…>                                  [ ✕ ]  │   ← Filters tab
 *   │  <kpis slot…>                                            │   ← KPIs tab
 *   └─────────────────────────────────────────────────────────┘
 *
 * - `open` is owned by the page (usually toggled by an "Acciones" button in
 *   the page header). When false the panel is hidden.
 * - `filters` and `kpis` are slots — the page composes the actual controls /
 *   cards. Wrap each filter control in <MakaFilterField label> so labels and
 *   controls line up perfectly across the row.
 * - `onClear` renders an aligned ✕ clear-all button at the end of the row.
 * - The KPIs tab only appears when `kpis` is provided.
 *
 * Theme-aware (CSS tokens) and i18n-aware (common.gridFilters.*).
 */
import { useId, useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";
import { Input } from "@/components/ui/input";

export interface MakaGridFiltersProps {
  /** Whether the panel is expanded. */
  open: boolean;
  /** Filter controls — wrap each in <MakaFilterField> for aligned labels. */
  filters: ReactNode;
  /** KPI cards. When omitted, the KPIs tab is not shown. */
  kpis?: ReactNode;
  /** When provided, an aligned ✕ button resets all filters. */
  onClear?: () => void;
  /** Tab selected on first render. Default "filters". */
  defaultTab?: "filters" | "kpis";
  className?: string;
}

export function MakaGridFilters({
  open,
  filters,
  kpis,
  onClear,
  defaultTab = "filters",
  className,
}: MakaGridFiltersProps) {
  const { t } = useTranslation("common");
  const [activeTab, setActiveTab] = useState<"filters" | "kpis">(defaultTab);
  const baseId = useId();

  if (!open) return null;

  const hasKpis = kpis !== undefined && kpis !== null;
  const tabs: Array<"filters" | "kpis"> = hasKpis ? ["filters", "kpis"] : ["filters"];
  const tabId = (tab: "filters" | "kpis") => `${baseId}-tab-${tab}`;
  const panelId = (tab: "filters" | "kpis") => `${baseId}-panel-${tab}`;

  return (
    <div className={cn("rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]", className)}>
      {/* Tab strip */}
      <div role="tablist" className="flex items-center gap-1 border-b border-[var(--color-border)] px-3 pt-2">
        {tabs.map((tab) => (
          <button
            key={tab}
            type="button"
            role="tab"
            id={tabId(tab)}
            aria-selected={activeTab === tab}
            aria-controls={panelId(tab)}
            onClick={() => setActiveTab(tab)}
            className={cn(
              "relative -mb-px rounded-t-md px-3.5 py-2 text-[13px] font-medium transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              activeTab === tab
                ? "border-b-2 border-[var(--color-primary)] text-[var(--color-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {tab === "filters" ? t("gridFilters.filtersTab") : t("gridFilters.kpisTab")}
          </button>
        ))}
      </div>

      {/* Filters tab — the controls wrap inside the flex-1 container; the
          clear ✕ is a sibling pinned top-right so it always sits at the end of
          the FIRST row regardless of how many rows the filters wrap into. */}
      {activeTab === "filters" && (
        <div role="tabpanel" id={panelId("filters")} aria-labelledby={tabId("filters")} className="flex items-start gap-4 p-4">
          <div className="flex flex-1 flex-wrap items-start gap-x-6 gap-y-4">
            {filters}
          </div>
          {onClear && (
            <MakaFilterField label="">
              <button
                type="button"
                onClick={onClear}
                title={t("grid.clearFilters")}
                aria-label={t("grid.clearFilters")}
                className={cn(
                  "grid size-8 place-items-center rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]",
                  "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                  "transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                )}
              >
                <X className="size-4" />
              </button>
            </MakaFilterField>
          )}
        </div>
      )}

      {/* KPIs tab */}
      {hasKpis && activeTab === "kpis" && (
        <div role="tabpanel" id={panelId("kpis")} aria-labelledby={tabId("kpis")} className="grid grid-cols-2 gap-3 p-4 sm:grid-cols-3 lg:grid-cols-5">
          {kpis}
        </div>
      )}
    </div>
  );
}

/**
 * Uniform filter field — an uppercase label sitting above a control, with a
 * fixed control-row height so every field (inputs, pills, date pickers, the
 * clear button) lines up on the same baseline. Pass label="" for a spacer
 * that keeps a control aligned without a visible caption.
 */
export function MakaFilterField({
  label,
  children,
  className,
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  const labelId = useId();
  const hasLabel = label.trim().length > 0;
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <span
        id={hasLabel ? labelId : undefined}
        className="text-[11px] font-semibold uppercase leading-4 tracking-wider text-[var(--color-muted-foreground)]">
        {label || " "}
      </span>
      {/* role="group" + aria-labelledby names the control(s) from the caption —
          uniform for text inputs, comboboxes and pill groups. */}
      <div
        className="flex min-h-8 items-center"
        {...(hasLabel ? { role: "group", "aria-labelledby": labelId } : {})}
      >
        {children}
      </div>
    </div>
  );
}

/**
 * Clearable filter text input — a search/text filter with an inline ✕ that
 * resets the value (appears only when there's something to clear). Same h-8
 * sizing as the other filter controls so a row of fields lines up.
 */
export function MakaFilterInput({
  value,
  onChange,
  placeholder,
  className,
  ariaLabel,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  ariaLabel?: string;
}) {
  const { t } = useTranslation("common");
  return (
    <div className="relative w-full">
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={ariaLabel}
        className={cn("h-8 w-full pr-8", className)}
      />
      {value && (
        <button
          type="button"
          onClick={() => onChange("")}
          aria-label={t("actions.clear")}
          className={cn(
            "absolute right-1.5 top-1/2 grid h-5 w-5 -translate-y-1/2 cursor-pointer place-items-center rounded",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          )}
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </div>
  );
}
