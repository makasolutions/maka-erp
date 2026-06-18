import { apiFetch } from "@/lib/api-client";

const BASE = "/api/v1/phones";

/**
 * Teléfono genérico polimórfico (control reutilizable). Se asocia a cualquier entidad vía
 * `ownerType` (string) + `ownerId`. Backend: módulo SharedRecords (PR-G2).
 */
export type PhoneDto = {
  id: string;
  ownerType: string;
  ownerId: string;
  typeCode: string | null;
  isActive: boolean;
  isPrimary: boolean;
  number: string;
  extension: string | null;
  countryCode: string | null;
};

/** Campos editables de un teléfono (sin owner/id — el owner es inmutable). */
export type PhoneWriteInput = {
  typeCode: string | null;
  isActive: boolean;
  isPrimary: boolean;
  number: string;
  extension: string | null;
  countryCode: string | null;
};

export function listPhones(ownerType: string, ownerId: string): Promise<PhoneDto[]> {
  const qs = new URLSearchParams({ ownerType, ownerId });
  return apiFetch(`${BASE}?${qs.toString()}`);
}

export const createPhone = (ownerType: string, ownerId: string, input: PhoneWriteInput): Promise<string> =>
  apiFetch(BASE, { method: "POST", body: JSON.stringify({ ownerType, ownerId, ...input }) });

export const updatePhone = (id: string, input: PhoneWriteInput): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "PUT", body: JSON.stringify({ id, ...input }) });

export const deletePhone = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "DELETE" });

export const setPrimaryPhone = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}/set-primary`, { method: "POST" });

/** Clave de React Query por owner. */
export const phonesQueryKey = (ownerType: string, ownerId: string) =>
  ["shared-records", "phones", ownerType, ownerId] as const;

export const emptyPhoneInput = (isPrimary: boolean): PhoneWriteInput => ({
  typeCode: null,
  isActive: true,
  isPrimary,
  number: "",
  extension: null,
  countryCode: null,
});
