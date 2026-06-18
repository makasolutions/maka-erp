import { apiFetch } from "@/lib/api-client";

const BASE = "/api/v1/addresses";

/**
 * Dirección genérica polimórfica (control reutilizable). Se asocia a cualquier entidad vía
 * `ownerType` (string) + `ownerId`. Backend: módulo SharedRecords (PR-G1).
 */
export type AddressDto = {
  id: string;
  ownerType: string;
  ownerId: string;
  labelCode: string | null;
  isActive: boolean;
  isPrimary: boolean;
  country: string;
  department: string | null;
  city: string | null;
  departmentCode: string | null;
  municipalityCode: string | null;
  line: string | null;
  barrio: string | null;
  reference: string | null;
  latitude: number | null;
  longitude: number | null;
};

/** Campos editables de una dirección (sin owner/id — el owner es inmutable). */
export type AddressWriteInput = {
  labelCode: string | null;
  isActive: boolean;
  isPrimary: boolean;
  country: string;
  department: string | null;
  city: string | null;
  departmentCode: string | null;
  municipalityCode: string | null;
  line: string | null;
  barrio: string | null;
  reference: string | null;
  latitude: number | null;
  longitude: number | null;
};

export function listAddresses(ownerType: string, ownerId: string): Promise<AddressDto[]> {
  const qs = new URLSearchParams({ ownerType, ownerId });
  return apiFetch(`${BASE}?${qs.toString()}`);
}

export const createAddress = (ownerType: string, ownerId: string, input: AddressWriteInput): Promise<string> =>
  apiFetch(BASE, { method: "POST", body: JSON.stringify({ ownerType, ownerId, ...input }) });

export const updateAddress = (id: string, input: AddressWriteInput): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "PUT", body: JSON.stringify({ id, ...input }) });

export const deleteAddress = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "DELETE" });

export const setPrimaryAddress = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}/set-primary`, { method: "POST" });

/** Clave de React Query por owner. */
export const addressesQueryKey = (ownerType: string, ownerId: string) =>
  ["shared-records", "addresses", ownerType, ownerId] as const;

export const emptyAddressInput = (isPrimary: boolean): AddressWriteInput => ({
  labelCode: null,
  isActive: true,
  isPrimary,
  country: "Colombia",
  department: null,
  city: null,
  departmentCode: null,
  municipalityCode: null,
  line: "",
  barrio: null,
  reference: null,
  latitude: null,
  longitude: null,
});
