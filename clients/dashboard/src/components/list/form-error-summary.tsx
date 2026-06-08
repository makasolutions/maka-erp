import { useTranslation } from "react-i18next";
import { AlertCircle } from "lucide-react";

/**
 * Reusable validation/error banner for dialog forms. Pass the field-error map
 * from a 400 ProblemDetails (`fieldErrors()`); for non-field errors (409, 500,
 * business rules) pass a friendly `message` (`describe()`). Renders one
 * destructive banner — field list when present, otherwise the single message.
 */
export function FormErrorSummary({
  errors,
  message,
}: {
  errors?: Record<string, string[]> | null;
  message?: string | null;
}) {
  const { t } = useTranslation("common");
  const fieldMessages = errors
    ? ([...new Set(Object.values(errors).flat().map((m) => m?.trim()).filter(Boolean))] as string[])
    : [];

  if (fieldMessages.length === 0 && !message?.trim()) return null;

  return (
    <div role="alert"
      className="mb-3 rounded-lg border border-[var(--color-destructive)]/40 bg-[var(--color-destructive)]/10 px-3 py-2.5">
      <div className="flex items-center gap-1.5 text-[12.5px] font-semibold text-[var(--color-destructive)]">
        <AlertCircle className="size-3.5" />
        {fieldMessages.length > 0
          ? t("validation.summaryTitle", { count: fieldMessages.length })
          : t("feedback.saveFailed")}
      </div>
      {fieldMessages.length > 0 ? (
        <ul className="mt-1 list-disc space-y-0.5 pl-6 text-[12px] text-[var(--color-destructive)]">
          {fieldMessages.map((m, i) => <li key={i}>{m}</li>)}
        </ul>
      ) : (
        <p className="mt-1 pl-5 text-[12px] text-[var(--color-destructive)]">{message}</p>
      )}
    </div>
  );
}
