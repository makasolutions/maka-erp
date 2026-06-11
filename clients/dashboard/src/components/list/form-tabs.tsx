import { useId, type ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * FormTabs — tabbed sections inside a form/dialog. Splits a long form into a few
 * coherent tabs (e.g. "General" / "Políticas" / "Reglas") so the dialog stays short
 * and scannable instead of an endless scroll.
 *
 * Design rules baked in:
 * - **All panels stay mounted** (inactive ones are `hidden`), so inputs keep their
 *   value and a submit-time validator can read every field regardless of the active
 *   tab. Never unmount panels in a form — you'd drop user input.
 * - Tabs can show a `badge` (e.g. a red dot when that tab has validation errors, or a
 *   count) so the user knows where to look without clicking through.
 * - Controlled: the page owns `active` + `onChange`.
 *
 * @example
 *   const [tab, setTab] = useState("general");
 *   <FormTabs active={tab} onChange={setTab} tabs={[
 *     { id: "general",  label: t("tabs.general"),  content: <FormGrid>…</FormGrid> },
 *     { id: "policies", label: t("tabs.policies"), content: <FormGrid>…</FormGrid> },
 *   ]} />
 */
export interface FormTab {
  id: string;
  label: string;
  /** Optional trailing adornment — a count pill or an error dot. */
  badge?: ReactNode;
  content: ReactNode;
}

export function FormTabs({
  tabs,
  active,
  onChange,
  className,
}: {
  tabs: FormTab[];
  active: string;
  onChange: (id: string) => void;
  className?: string;
}) {
  const base = useId();
  return (
    <div className={className}>
      <div role="tablist" className="mb-4 flex flex-wrap items-center gap-1 border-b border-[var(--color-border)]">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`${base}-t-${tab.id}`}
            aria-selected={active === tab.id}
            aria-controls={`${base}-p-${tab.id}`}
            onClick={() => onChange(tab.id)}
            className={cn(
              "relative -mb-px flex items-center gap-1.5 rounded-t-md px-3.5 py-2 text-[13px] font-medium transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              active === tab.id
                ? "border-b-2 border-[var(--color-primary)] text-[var(--color-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {tab.label}
            {tab.badge}
          </button>
        ))}
      </div>
      {tabs.map((tab) => (
        <div
          key={tab.id}
          role="tabpanel"
          id={`${base}-p-${tab.id}`}
          aria-labelledby={`${base}-t-${tab.id}`}
          hidden={active !== tab.id}
        >
          {tab.content}
        </div>
      ))}
    </div>
  );
}
