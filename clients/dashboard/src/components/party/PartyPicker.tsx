import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Combobox } from "@/components/list";
import { searchParties } from "@/api/parties";

export interface PartyPickerProps {
  id: string;
  value: string | null;
  onChange: (partyId: string | null) => void;
  label?: string;
  /** Optional role filter ("Customer" | "Supplier"). */
  role?: "Customer" | "Supplier";
  disabled?: boolean;
}

/** Searchable select of terceros by name/NIT — reusable in Orders, Cotizaciones, Billing. */
export function PartyPicker({ id, value, onChange, label, role, disabled }: PartyPickerProps) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const { data } = useQuery({
    queryKey: ["crm", "parties", "picker", role ?? "all"],
    queryFn: () => searchParties({ pageSize: 200, sort: "legalName", role: role ?? null }),
    staleTime: 60 * 1000,
  });
  const options = useMemo(
    () => (data?.items ?? []).map((p) => ({
      value: p.id,
      label: p.legalName,
      hint: `${p.identificationTypeCode} ${p.identificationNumber}`,
    })),
    [data],
  );

  // The noun is role-aware so the trigger placeholder AND the Combobox filter stay
  // consistent ("Buscar proveedor…" ↔ "Filtrar proveedor…"), instead of a generic
  // "tercero" that contradicts a Proveedor/Cliente context.
  const roleNoun =
    role === "Supplier" ? t("parties.role.supplier")
    : role === "Customer" ? t("parties.role.customer")
    : t("parties.singular");
  const noun = label ?? roleNoun;
  return (
    <Combobox id={id} label={noun} value={value} onChange={onChange}
      options={options} searchable clearable disabled={disabled} placeholder={`${tc("actions.search")} ${noun.toLowerCase()}…`} />
  );
}
