import { useMemo } from "react";
import { Combobox } from "@/components/list";
import { useBasicRecords } from "./use-basic-records";

export interface BasicRecordSelectProps {
  id: string;
  /** Code of the Tabla Básica that backs this dropdown (e.g. "IdentificationType"). */
  tableCode: string;
  /** Selected record Code (stable key), or null. */
  value: string | null;
  onChange: (code: string | null) => void;
  label: string;
  placeholder?: string;
  disabled?: boolean;
  clearable?: boolean;
  searchable?: boolean;
}

/**
 * Dropdown bound to a Tabla Básica: shows each record's Value, stores its Code.
 * Reusable across every form (Parties, Orders, Billing…). Backed by useBasicRecords.
 */
export function BasicRecordSelect({
  id, tableCode, value, onChange, label, placeholder, disabled,
  clearable = true, searchable = true,
}: BasicRecordSelectProps) {
  const { data = [] } = useBasicRecords(tableCode);
  const options = useMemo(
    () => data.map((r) => ({ value: r.code, label: r.value })),
    [data],
  );

  return (
    <Combobox
      id={id}
      label={label}
      value={value}
      onChange={onChange}
      options={options}
      placeholder={placeholder}
      disabled={disabled}
      clearable={clearable}
      searchable={searchable}
    />
  );
}
