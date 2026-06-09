import { useTranslation } from "react-i18next";
import { Combobox, Field, type FormSpan } from "@/components/list";
import { useColombiaGeo } from "./use-colombia-geo";

export interface CityValue {
  departmentCode?: string | null;
  municipalityCode?: string | null;
  department?: string | null;
  city?: string | null;
}

export interface CityPickerProps {
  idPrefix: string;
  value: CityValue;
  onChange: (patch: CityValue) => void;
  disabled?: boolean;
  /** error for the municipality (city) field */
  cityError?: string;
  /** Tailwind span for each of the two Fields (department + municipality). */
  span?: FormSpan;
}

/**
 * DIVIPOLA cascade picker: Department → Municipality. Stores both the human-readable
 * names (department/city) and the DIVIPOLA codes (departmentCode/municipalityCode) so the
 * backend can resolve and normalize the address. Renders two <Field> grid items.
 */
export function CityPicker({ idPrefix, value, onChange, disabled, cityError, span = 4 }: CityPickerProps) {
  const { t } = useTranslation("crm");
  const { departments, municipalitiesOf } = useColombiaGeo();

  const munis = municipalitiesOf(value.departmentCode);

  const setDepartment = (code: string | null) => {
    const dept = departments.find((d) => d.code === code);
    onChange({ departmentCode: code, department: dept?.name ?? null, municipalityCode: null, city: null });
  };
  const setMunicipality = (code: string | null) => {
    const mun = munis.find((m) => m.code === code);
    onChange({ municipalityCode: code, city: mun?.name ?? null });
  };

  return (
    <>
      <Field id={`${idPrefix}-dept`} span={span} label={t("parties.address.department")}>
        <Combobox
          id={`${idPrefix}-dept`}
          label={t("parties.address.department")}
          value={value.departmentCode ?? null}
          onChange={setDepartment}
          options={departments.map((d) => ({ value: d.code, label: d.name }))}
          searchable
          clearable
          placeholder={t("parties.address.department")}
          disabled={disabled}
        />
      </Field>
      <Field id={`${idPrefix}-city`} span={span} label={t("parties.address.city")} required error={cityError}>
        <Combobox
          id={`${idPrefix}-city`}
          label={t("parties.address.city")}
          value={value.municipalityCode ?? null}
          onChange={setMunicipality}
          options={munis.map((m) => ({ value: m.code, label: m.name }))}
          searchable
          clearable
          placeholder={value.departmentCode ? t("parties.address.city") : t("parties.address.cityPickDept")}
          disabled={disabled || !value.departmentCode}
        />
      </Field>
    </>
  );
}
