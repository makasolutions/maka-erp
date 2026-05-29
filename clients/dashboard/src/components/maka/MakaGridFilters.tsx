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
import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";

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

  if (!open) return null;

  const hasKpis = kpis !== undefined && kpis !== null;
  const tabs: Array<"filters" | "kpis"> = hasKpis ? ["filters", "kpis"] : ["filters"];

  return (
    <div className={cn("rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]", className)}>
      {/* Tab strip */}
      <div className="flex items-center gap-1 border-b border-[var(--color-border)] px-3 pt-2">
        {tabs.map((tab) => (
          <button
            key={tab}
            type="button"
            onClick={() => setActiveTab(tab)}
            className={cn(
              "relative -mb-px rounded-t-md px-3.5 py-2 text-[13px] font-medium transition-colors",
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
        <div className="flex items-start gap-4 p-4">
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
        <div className="grid grid-cols-2 gap-3 p-4 sm:grid-cols-3 lg:grid-cols-5">
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
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <span className="text-[11px] font-semibold uppercase leading-4 tracking-wider text-[var(--color-muted-foreground)]">
        {label || " "}
      </span>
      <div className="flex min-h-8 items-center">{children}</div>
    </div>
  );
}
