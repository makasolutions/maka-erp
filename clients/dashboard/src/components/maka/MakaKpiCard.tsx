/**
 * MakaKpiCard — the single, reusable KPI tile shown in the "Indicadores" tab of
 * MakaGridFilters. Replaces the per-page local `KpiCard` copies (§18.2 reuse mandate).
 *
 * House rule: **every KPI carries an icon**. Pass a lucide icon component (`icon`) plus a
 * semantic `tone` token; the icon sits in a tinted chip so KPIs read consistently across
 * every grid screen. `value` is formatted with the es-CO grouping when numeric.
 *
 * @example
 *   import { MakaKpiCard } from "@/components/maka";
 *   import { Package } from "lucide-react";
 *   <MakaKpiCard icon={Package} label={t("brands.kpi.total")} value={total} tone="var(--color-primary)" />
 */
import type { ComponentType } from "react";

export interface MakaKpiCardProps {
  /** lucide-react icon component (e.g. `Package`). Required — every KPI has an icon. */
  icon: ComponentType<{ className?: string }>;
  label: string;
  value: number | string;
  /** Semantic color token, e.g. `var(--color-primary)`. Defaults to the accent. */
  tone?: string;
  /** Optional small caption under the value (e.g. "+12% vs. mes anterior"). */
  hint?: string;
}

export function MakaKpiCard({
  icon: Icon,
  label,
  value,
  tone = "var(--color-primary)",
  hint,
}: MakaKpiCardProps) {
  const display = typeof value === "number" ? value.toLocaleString("es-CO") : value;
  return (
    <div className="flex items-center gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-4 py-3">
      <span
        aria-hidden
        className="grid size-9 shrink-0 place-items-center rounded-[8px]"
        style={{
          backgroundColor: `color-mix(in oklab, ${tone} 12%, var(--color-card))`,
          color: tone,
          boxShadow: `inset 0 0 0 1px color-mix(in oklab, ${tone} 22%, transparent)`,
        }}
      >
        <Icon className="size-4" />
      </span>
      <div className="flex min-w-0 flex-col">
        <span className="truncate text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {label}
        </span>
        <span className="font-display text-[22px] font-semibold leading-tight tabular-nums text-[var(--color-foreground)]">
          {display}
        </span>
        {hint && (
          <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">{hint}</span>
        )}
      </div>
    </div>
  );
}
