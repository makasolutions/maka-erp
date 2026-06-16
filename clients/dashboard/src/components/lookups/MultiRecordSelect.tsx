import { useMemo } from "react";
import { X } from "lucide-react";
import { Combobox } from "@/components/list";
import { Badge } from "@/components/ui/badge";
import { useBasicRecords } from "./use-basic-records";

export interface MultiRecordSelectProps {
  id: string;
  /** Code of the Tabla Básica that backs this control (e.g. "FiscalResponsibility"). */
  tableCode: string;
  /** Selected record Codes. */
  value: string[];
  onChange: (codes: string[]) => void;
  label: string;
  placeholder?: string;
  disabled?: boolean;
}

/**
 * Multi-select sobre una Tabla Básica: chips (Badge + ✕) para los seleccionados + un Combobox que
 * agrega uno a la vez (excluyendo los ya elegidos). Reutilizable (lo usarán también las CIIU
 * múltiples). Tailwind nativo + tokens de tema — sin Syncfusion (es composición de primitivos).
 */
export function MultiRecordSelect({
  id, tableCode, value, onChange, label, placeholder, disabled,
}: MultiRecordSelectProps) {
  const { data = [] } = useBasicRecords(tableCode);

  const labelByCode = useMemo(() => {
    const m = new Map<string, string>();
    for (const r of data) m.set(r.code, r.value);
    return m;
  }, [data]);

  const available = useMemo(
    () => data.filter((r) => !value.includes(r.code)).map((r) => ({ value: r.code, label: r.value })),
    [data, value],
  );

  const add = (code: string | null) => {
    if (code && !value.includes(code)) onChange([...value, code]);
  };
  const remove = (code: string) => onChange(value.filter((c) => c !== code));

  return (
    <div className="space-y-2">
      {value.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {value.map((code) => (
            <Badge key={code} variant="outline" className="gap-1 pr-1">
              {labelByCode.get(code) ?? code}
              {!disabled && (
                <button type="button" onClick={() => remove(code)} aria-label={`${label}: ${labelByCode.get(code) ?? code}`}
                  className="rounded-sm p-0.5 text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]">
                  <X className="size-3" />
                </button>
              )}
            </Badge>
          ))}
        </div>
      )}
      <Combobox
        id={id}
        label={label}
        value={null}
        onChange={add}
        options={available}
        placeholder={placeholder}
        searchable
        disabled={disabled || available.length === 0}
      />
    </div>
  );
}
