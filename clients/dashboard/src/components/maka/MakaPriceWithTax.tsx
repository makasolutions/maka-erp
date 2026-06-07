/**
 * MakaPriceWithTax — money control that shows base ↔ IVA-included (19%) side by side.
 *
 * Three cells share one field width: [base] [19%] [IVA incl.]. Editing the base
 * computes the VAT-included value; editing the VAT-included computes the base.
 * The value the caller stores is always the **base** price (sin IVA). Used in the
 * product form, price lists and campaigns so every money input behaves the same.
 */
import { useTranslation } from "react-i18next";
import { MakaCurrencyInput } from "./MakaCurrencyInput";
import { IVA_RATE } from "@/lib/list-helpers";

export interface MakaPriceWithTaxProps {
  id?: string;
  /** Base price (sin IVA) as a string; "" when empty. */
  value: string;
  /** Receives the new base price as a string ("" when cleared). */
  onChange: (base: string) => void;
  disabled?: boolean;
}

export function MakaPriceWithTax({ id, value, onChange, disabled }: MakaPriceWithTaxProps) {
  const { t } = useTranslation("catalog");
  const baseNum = value === "" ? null : Number(value);
  const ivaNum = baseNum == null ? null : Math.round(baseNum * (1 + IVA_RATE));

  return (
    <div>
      <div className="flex items-stretch gap-1">
        <div className="min-w-0 flex-1">
          <MakaCurrencyInput id={id} value={baseNum} disabled={disabled}
            onChange={(n) => onChange(n == null ? "" : String(n))} ariaLabel={t("priceLists.base")} />
        </div>
        <span aria-hidden className="grid w-10 shrink-0 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] text-[11px] font-semibold text-[var(--color-muted-foreground)]">
          19%
        </span>
        <div className="min-w-0 flex-1">
          <MakaCurrencyInput value={ivaNum} disabled={disabled}
            onChange={(n) => onChange(n == null ? "" : String(Math.round(n / (1 + IVA_RATE))))} ariaLabel={t("priceLists.withTax")} />
        </div>
      </div>
      <div className="mt-0.5 flex gap-1 text-[10px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
        <span className="flex-1">{t("priceLists.base")}</span>
        <span className="w-10 shrink-0 text-center">{t("priceLists.vatLabel")}</span>
        <span className="flex-1">{t("priceLists.withTax")}</span>
      </div>
    </div>
  );
}
