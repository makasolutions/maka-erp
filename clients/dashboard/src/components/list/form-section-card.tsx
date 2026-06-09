import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * FormSectionCard — tarjeta de sección para formularios/popups. Da una separación
 * **consistente** entre bloques de un formulario (reemplaza los `border-t` ad-hoc):
 * un encabezado minimalista (ícono + título mono-caps + descripción opcional) sobre
 * una superficie `Card`. Pensada para envolver un `FormGrid`.
 *
 * Uso:
 *   <FormSectionCard icon={MapPin} title={t("...")} description={t("...")}>
 *     <FormGrid> … </FormGrid>
 *   </FormSectionCard>
 */
export function FormSectionCard({
  icon: Icon,
  title,
  description,
  actions,
  invalid,
  className,
  children,
}: {
  icon?: LucideIcon;
  title: string;
  description?: string;
  /** Optional right-aligned controls in the header (e.g. an "add" button). */
  actions?: React.ReactNode;
  /** When true, tints the header to flag a section with validation errors. */
  invalid?: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section
      className={cn(
        "rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] shadow-sm",
        className,
      )}
    >
      <header className="flex items-start justify-between gap-3 border-b border-[var(--color-border)] px-4 py-3 sm:px-5">
        <div className="flex items-center gap-2">
          {Icon && (
            <span
              className={cn(
                "grid size-7 shrink-0 place-items-center rounded-md border",
                invalid
                  ? "border-[var(--color-destructive)]/30 bg-[var(--color-destructive)]/10 text-[var(--color-destructive)]"
                  : "border-[var(--color-border)] bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
              )}
            >
              <Icon className="size-4" />
            </span>
          )}
          <div className="min-w-0">
            <h3 className="text-[12px] font-semibold uppercase tracking-wider text-[var(--color-foreground)]">
              {title}
            </h3>
            {description && (
              <p className="mt-0.5 text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
                {description}
              </p>
            )}
          </div>
        </div>
        {actions && <div className="flex shrink-0 items-center gap-2">{actions}</div>}
      </header>
      <div className="px-4 py-4 sm:px-5">{children}</div>
    </section>
  );
}
