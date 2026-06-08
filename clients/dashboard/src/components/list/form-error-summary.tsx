import { useTranslation } from "react-i18next";
import { AlertCircle } from "lucide-react";

/**
 * Reusable validation-error banner for dialog forms. Pass the field-error map
 * from a 400 ProblemDetails (see `fieldErrors()` in list-helpers). Renders a
 * localized header + a bullet list of all messages. Use across forms for a
 * consistent, site-wide error presentation.
 */
export function FormErrorSummary({ errors }: { errors?: Record<string, string[]> | null }) {
  const { t } = useTranslation("common");
  if (!errors) return null;
  const messages = [...new Set(Object.values(errors).flat().map((m) => m?.trim()).filter(Boolean))] as string[];
  if (messages.length === 0) return null;

  return (
    <div role="alert"
      className="mb-3 rounded-lg border border-[var(--color-destructive)]/40 bg-[var(--color-destructive)]/10 px-3 py-2.5">
      <div className="flex items-center gap-1.5 text-[12.5px] font-semibold text-[var(--color-destructive)]">
        <AlertCircle className="size-3.5" />
        {t("validation.summaryTitle", { count: messages.length })}
      </div>
      <ul className="mt-1 list-disc space-y-0.5 pl-6 text-[12px] text-[var(--color-destructive)]">
        {messages.map((m, i) => <li key={i}>{m}</li>)}
      </ul>
    </div>
  );
}
