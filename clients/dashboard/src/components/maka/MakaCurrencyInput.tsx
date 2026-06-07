/**
 * MakaCurrencyInput — Syncfusion NumericTextBox formatted as Colombian pesos.
 *
 * Guarantees currency formatting inside the input ("$1.234.567") by loading the
 * es-CO CLDR data and using format="c0" / currency="COP". Use this anywhere a
 * money amount is typed so every screen formats consistently (house rule:
 * `$` first, dot thousands, no decimals for COP).
 */
import { NumericTextBoxComponent } from "@syncfusion/ej2-react-inputs";
import { loadCldr, setCulture, setCurrencyCode } from "@syncfusion/ej2-base";
import { cn } from "@/lib/cn";

// CLDR for es-CO (Colombia): "$" before, "." thousands, no decimals.
import numberingSystems from "cldr-data/supplemental/numberingSystems.json";
import currencyData from "cldr-data/supplemental/currencyData.json";
import esNumbers from "cldr-data/main/es-CO/numbers.json";
import esCurrencies from "cldr-data/main/es-CO/currencies.json";

let loaded = false;
function ensureCulture() {
  if (loaded) return;
  loadCldr(numberingSystems, currencyData, esNumbers, esCurrencies);
  setCulture("es-CO");
  setCurrencyCode("COP");
  loaded = true;
}
ensureCulture();

export interface MakaCurrencyInputProps {
  id?: string;
  value: number | null;
  onChange: (value: number | null) => void;
  disabled?: boolean;
  placeholder?: string;
  ariaLabel?: string;
  className?: string;
  min?: number;
}

export function MakaCurrencyInput({
  id, value, onChange, disabled, placeholder, ariaLabel, className, min = 0,
}: MakaCurrencyInputProps) {
  return (
    <NumericTextBoxComponent
      id={id}
      value={value ?? undefined}
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      change={(e: any) => onChange(e?.value ?? null)}
      enabled={!disabled}
      placeholder={placeholder}
      format="c0"
      currency="COP"
      locale="es-CO"
      decimals={0}
      validateDecimalOnType
      min={min}
      showSpinButton={false}
      cssClass={cn("maka-currency", className)}
      aria-label={ariaLabel}
    />
  );
}
