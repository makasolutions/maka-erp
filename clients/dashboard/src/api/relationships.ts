import { apiFetch } from "@/lib/api-client";

/**
 * PartyRelationship M2M (persona↔empresa) — PR-3 ContactList. Dirección A (empresa→contactos, editable)
 * y dirección B (persona→empresas, solo lectura). Valores de custom fields (scope PartyRelationship)
 * viajan como objeto `{ slug: valor }`.
 */

/** Custom field values keyed by apiSlug (mismo shape que el CustomFieldRenderer de PR-1). */
export type CustomFieldValues = Record<string, unknown>;

/** Empresa→contacto (dirección A): datos de la persona-Source resueltos en el backend. */
export type PartyRelationshipDto = {
  id: string;
  sourcePartyId: string;
  sourceName?: string | null;
  sourcePrimaryChannel?: string | null;
  sourceIsPEP: boolean;
  targetPartyId: string;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  startDate: string;
  endDate?: string | null;
  /** Valores de custom fields tal como están en el JSONB (string JSON, se parsea para el editor). */
  customFieldsJson?: string | null;
};

/** Persona→empresa (dirección B, solo lectura): nombre de la empresa-Target resuelto en backend. */
export type PartyRelationshipBySourceDto = {
  id: string;
  targetPartyId: string;
  targetName?: string | null;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  startDate: string;
  endDate?: string | null;
};

/** Persona nueva mínima "stageada" en el buscar-o-crear (se crea atómicamente en el backend). */
export type NewPersonInput = {
  identificationTypeCode: string;
  identificationNumber: string;
  verificationDigit?: number | null;
  firstName?: string | null;
  lastName?: string | null;
  legalName?: string | null;
};

/** Línea de relación para la creación atómica del tercero (Opción B): persona existente O nueva inline. */
export type PartyRelationshipLineInput = {
  sourcePartyId?: string | null;
  newPerson?: NewPersonInput | null;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  startDate?: string | null;
  endDate?: string | null;
  customFields?: CustomFieldValues | null;
};

// ─────────────────────────── Dirección A (empresa→contactos) ───────────────────────────

export function getRelationshipsByTarget(
  targetPartyId: string, includeInactive = false,
): Promise<PartyRelationshipDto[]> {
  const q = new URLSearchParams({ targetPartyId });
  if (includeInactive) q.set("includeInactive", "true");
  return apiFetch<PartyRelationshipDto[]>(`/api/v1/parties/relationships?${q.toString()}`);
}

export type CreateRelationshipInput = {
  sourcePartyId?: string | null;
  newPerson?: NewPersonInput | null;
  targetPartyId: string;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  startDate?: string | null;
  endDate?: string | null;
  customFields?: CustomFieldValues | null;
};

export function createRelationship(input: CreateRelationshipInput): Promise<string> {
  return apiFetch<string>("/api/v1/parties/relationships", { method: "POST", body: JSON.stringify(input) });
}

export type UpdateRelationshipInput = {
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  startDate: string;
  endDate?: string | null;
  customFields?: CustomFieldValues | null;
};

export async function updateRelationship(id: string, input: UpdateRelationshipInput): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/relationships/${encodeURIComponent(id)}`, {
    method: "PUT", body: JSON.stringify({ id, ...input }),
  });
}

export async function setPrimaryRelationship(id: string, isPrimary: boolean): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/relationships/${encodeURIComponent(id)}/primary`, {
    method: "PUT", body: JSON.stringify({ id, isPrimary }),
  });
}

export async function deleteRelationship(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/relationships/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// ─────────────────────────── Dirección B (persona→empresas, read-only) ───────────────────────────

export function getRelationshipsBySource(
  sourcePartyId: string, includeInactive = false,
): Promise<PartyRelationshipBySourceDto[]> {
  const q = new URLSearchParams({ sourcePartyId });
  if (includeInactive) q.set("includeInactive", "true");
  return apiFetch<PartyRelationshipBySourceDto[]>(`/api/v1/parties/relationships/by-source?${q.toString()}`);
}
