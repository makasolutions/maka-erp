import { useTranslation } from "react-i18next";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";

/**
 * 12-column form span (Bootstrap-style). On mobile every field is full width;
 * the span applies from the `sm` breakpoint up. Classes are spelled out
 * statically so Tailwind's JIT scanner can see them — never build them
 * dynamically.
 *
 * House convention for editor dialogs (see CLAUDE.md):
 *   code → 4, name → 8, slug → 6, description → 12, most other fields → 6.
 */
export type FormSpan = 2 | 3 | 4 | 6 | 8 | 12;

const SPAN_CLASS: Record<FormSpan, string> = {
  2: "sm:col-span-2",
  3: "sm:col-span-3",
  4: "sm:col-span-4",
  6: "sm:col-span-6",
  8: "sm:col-span-8",
  12: "sm:col-span-12",
};

/**
 * Two-column responsive form grid for popup/editor dialogs. Wrap `Field`s (or
 * any control) in it and give each a `span` so rows tile to 12 columns. Single
 * column below `sm`.
 */
export function FormGrid({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("grid grid-cols-1 gap-x-4 gap-y-5 sm:grid-cols-12", className)}>
      {children}
    </div>
  );
}

/**
 * Form field wrapper used across editor dialogs. Mono-caps tracked
 * label, optional `*` hint that's a primary-toned middle dot, and a
 * faint hint line below the control. The required indicator is
 * announced to screen readers via an sr-only "required" sibling so
 * the visual `·` glyph isn't relied on for semantics.
 *
 * Pass `span` when the field sits inside a {@link FormGrid} to set its
 * 12-column width.
 */
export function Field({
  id,
  label,
  hint,
  required,
  span,
  className,
  error,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  required?: boolean;
  span?: FormSpan;
  className?: string;
  /** When set, the control is flagged invalid and the message replaces the hint. */
  error?: string | null;
  children: React.ReactNode;
}) {
  const { t } = useTranslation("common");
  return (
    <div
      className={cn(
        "space-y-1.5",
        span && ["col-span-1", SPAN_CLASS[span]],
        // Tint any descendant input/control border when invalid.
        error &&
          "[&_input]:!border-[var(--color-destructive)] [&_textarea]:!border-[var(--color-destructive)] [&_[role=combobox]]:!border-[var(--color-destructive)] [&_button[role=combobox]]:!border-[var(--color-destructive)]",
        className,
      )}
    >
      <Label
        htmlFor={id}
        className="flex items-center gap-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
      >
        {label}
        {required && (
          <>
            <span aria-hidden className="text-[var(--color-destructive)]">·</span>
            <span className="sr-only">{t("fields.required")}</span>
          </>
        )}
      </Label>
      {children}
      {error ? (
        <p className="text-[11.5px] font-medium leading-relaxed text-[var(--color-destructive)]">
          {error}
        </p>
      ) : hint ? (
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]/85">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
