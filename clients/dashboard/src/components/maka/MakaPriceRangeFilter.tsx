/**
 * MakaPriceRangeFilter — reusable min/max currency range filter.
 * Two MakaCurrencyInput fields (COP-formatted) plus a ✕ to clear both. Reuse it
 * for any price-range filter so the formatting/clearing behaviour is consistent.
 */
import { X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { MakaCurrencyInput } from "./MakaCurrencyInput";

export interface MakaPriceRange {
  min: number | null;
  max: number | null;
}

export interface MakaPriceRangeFilterProps {
  value: MakaPriceRange;
  onChange: (value: MakaPriceRange) => void;
  className?: string;
}

export function MakaPriceRangeFilter({ value, onChange, className }: MakaPriceRangeFilterProps) {
  const { t } = useTranslation("common");
  const hasValue = value.min != null || value.max != null;

  return (
    <div className={`flex items-center gap-1.5 ${className ?? ""}`}>
      <div className="w-32">
        <MakaCurrencyInput value={value.min} onChange={(n) => onChange({ ...value, min: n })}
          placeholder={t("priceRange.min", "Mín")} ariaLabel={t("priceRange.min", "Mín")} />
      </div>
      <span aria-hidden className="text-[var(--color-muted-foreground)]">–</span>
      <div className="w-32">
        <MakaCurrencyInput value={value.max} onChange={(n) => onChange({ ...value, max: n })}
          placeholder={t("priceRange.max", "Máx")} ariaLabel={t("priceRange.max", "Máx")} />
      </div>
      {hasValue && (
        <button type="button" onClick={() => onChange({ min: null, max: null })}
          aria-label={t("actions.clear", "Limpiar")}
          className="grid size-7 shrink-0 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]">
          <X className="size-4" />
        </button>
      )}
    </div>
  );
}
