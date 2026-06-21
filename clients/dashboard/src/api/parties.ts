import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";
import type { PartyRelationshipLineInput } from "@/api/relationships";

export type PartyKind = "Natural" | "Juridica";
export type PartyStatus = "Active" | "Inactive" | "Prospect";
export type LifecycleStage = "Lead" | "Mql" | "Sql" | "Opportunity" | "Customer" | "Inactive";
export type PartyTeamRole = "Owner" | "Member";

/** Ejes fiscales v2 (SPEC §4/§13) — ortogonales (régimen de renta ⟂ responsabilidad de IVA). */
export type RegimenTributario = "Ordinario" | "Simple" | "Especial";
export type ResponsabilidadIVA = "Responsable" | "NoResponsable";

/** Input autoritativo de ejes fiscales — alternativa al taxRegimeCode plano legacy. */
export type PartyFiscalAxesInput = {
  regimenTributario: RegimenTributario | null;
  responsabilidadIVA: ResponsabilidadIVA | null;
  responsabilidadesFiscales: string[];
};

/** Porción fiscal del sub-objeto V2 del detalle (lo único que el tab Tributaria consume hoy). */
export type PartyV2FiscalView = {
  regimenTributario: RegimenTributario | null;
  responsabilidadIVA: ResponsabilidadIVA | null;
  responsabilidadesFiscales: string[];
};
export type PartyV2DetailView = { fiscal: PartyV2FiscalView | null };

/** [Flags] enum serialized as string ("None" | "Customer" | "Supplier" | "Customer, Supplier"). */
export type PartyRoles = string;

export function hasRole(roles: PartyRoles | undefined, role: "Customer" | "Supplier"): boolean {
  return !!roles && roles.includes(role);
}
export function rolesToApi(customer: boolean, supplier: boolean): PartyRoles {
  const parts = [customer ? "Customer" : null, supplier ? "Supplier" : null].filter(Boolean);
  return parts.length ? parts.join(", ") : "None";
}

export type PartyDto = {
  id: string;
  identificationTypeCode: string;
  identificationNumber: string;
  verificationDigit?: number | null;
  kind: PartyKind;
  legalName: string;
  tradeName?: string | null;
  roles: PartyRoles;
  status: PartyStatus;
  stage: LifecycleStage;
  email?: string | null;
  city?: string | null;
  assignedUserId?: string | null;
  createdAtUtc: string;
};

export type PartyBriefDto = {
  id: string; legalName: string; identificationTypeCode: string; identificationNumber: string; roles: PartyRoles;
};

export type PartyAddress = {
  id?: string; country: string; department?: string | null; city?: string | null; line?: string | null;
  barrio?: string | null; reference?: string | null; latitude?: number | null; longitude?: number | null;
  isPrimary: boolean; labelCode?: string | null;
  departmentCode?: string | null; municipalityCode?: string | null; normalizedLine?: string | null;
};
// PR-2: PartyContact (contactos v1) ELIMINADO — los contactos persona↔empresa son PartyRelationship
// (M2M), gestionados por ContactList (PR-3), no como hijos del comando de Party.
export type PartyChannel = {
  id?: string; channelTypeCode: string; value: string; reference?: string | null; isPrimary: boolean;
};
export type PartyTeamMember = { id?: string; userId: string; role: PartyTeamRole };

export type PartyDetailDto = {
  id: string;
  identificationTypeCode: string;
  identificationNumber: string;
  verificationDigit?: number | null;
  kind: PartyKind;
  legalName: string;
  firstName?: string | null;
  lastName?: string | null;
  tradeName?: string | null;
  email?: string | null;
  website?: string | null;
  taxRegimeCode?: string | null;
  fiscalResponsibilities?: string | null;
  actividadEconomicaCiiuCode?: string | null;
  roles: PartyRoles;
  status: PartyStatus;
  stage: LifecycleStage;
  leadScore: number;
  sourceCode?: string | null;
  assignedUserId?: string | null;
  marketingType?: string | null;
  birthDate?: string | null;
  genderCode?: string | null;
  maritalStatusCode?: string | null;
  hasCredit: boolean;
  creditLimit?: number | null;
  creditDaysCode?: string | null;
  creditBlocked: boolean;
  creditCurrency?: string | null;
  notes?: string | null;
  branchId?: string | null;
  isGlobalSupplier: boolean;
  addresses: PartyAddress[];
  channels: PartyChannel[];
  team: PartyTeamMember[];
  /** Sub-objeto v2 anidado (el backend lo expone; el front consume solo `fiscal` por ahora). */
  v2?: PartyV2DetailView | null;
};

export type SearchPartiesParams = {
  search?: string;
  kind?: PartyKind | null;
  role?: "Customer" | "Supplier" | null;
  status?: PartyStatus | null;
  stage?: LifecycleStage | null;
  city?: string;
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
};

export function searchParties(params: SearchPartiesParams = {}): Promise<PagedResponse<PartyDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.kind) q.set("kind", params.kind);
  if (params.role) q.set("role", params.role);
  if (params.status) q.set("status", params.status);
  if (params.stage) q.set("stage", params.stage);
  if (params.city) q.set("city", params.city);
  if (params.pageNumber) q.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) q.set("pageSize", String(params.pageSize));
  if (params.sort) q.set("sort", params.sort);
  const qs = q.toString();
  return apiFetch<PagedResponse<PartyDto>>(`/api/v1/parties${qs ? `?${qs}` : ""}`);
}

export function getPartyById(id: string): Promise<PartyDetailDto> {
  return apiFetch<PartyDetailDto>(`/api/v1/parties/${encodeURIComponent(id)}`);
}

export type PartyWriteInput = {
  identificationTypeCode: string;
  identificationNumber: string;
  verificationDigit?: number | null;
  kind: PartyKind;
  legalName: string;
  firstName?: string | null;
  lastName?: string | null;
  roles: PartyRoles;
  tradeName?: string | null;
  email?: string | null;
  website?: string | null;
  /** Legacy plano — el front lo manda null; la fuente de verdad fiscal es `fiscalAxes`. */
  taxRegimeCode?: string | null;
  fiscalResponsibilities?: string | null;
  actividadEconomicaCiiuCode?: string | null;
  status: PartyStatus;
  stage: LifecycleStage;
  leadScore: number;
  sourceCode?: string | null;
  assignedUserId?: string | null;
  marketingType?: string | null;
  birthDate?: string | null;
  genderCode?: string | null;
  maritalStatusCode?: string | null;
  hasCredit: boolean;
  creditLimit?: number | null;
  creditDaysCode?: string | null;
  creditBlocked: boolean;
  creditCurrency?: string | null;
  notes?: string | null;
  branchId?: string | null;
  addresses: PartyAddress[];
  channels: PartyChannel[];
  team: PartyTeamMember[];
  /** Ejes fiscales v2 autoritativos (el backend los prefiere sobre taxRegimeCode). */
  fiscalAxes?: PartyFiscalAxesInput;
  /** PR-3 (Opción B): contactos acumulados en el wizard, persistidos atómicamente al CREAR.
   *  Solo aplica en createParty; en edición los contactos se gestionan live (no se envía acá). */
  relationships?: PartyRelationshipLineInput[];
};

export function createParty(input: PartyWriteInput): Promise<string> {
  return apiFetch<string>("/api/v1/parties", { method: "POST", body: JSON.stringify(input) });
}

export async function updateParty(id: string, input: PartyWriteInput): Promise<void> {
  // identificationType/number are immutable on update; the command omits them. Relationships are
  // managed live in edit mode (not part of the UpdateParty command), so they are stripped here.
  const { identificationTypeCode: _t, identificationNumber: _n, relationships: _r, ...rest } = input;
  void _t; void _n; void _r;
  await apiFetch<void>(`/api/v1/parties/${encodeURIComponent(id)}`, {
    method: "PUT", body: JSON.stringify({ id, ...rest }),
  });
}

export async function deleteParty(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export type VerifyIdentificationResult = {
  valid: boolean;
  error?: string | null;
  verificationDigit?: number | null;
  legalName?: string | null;
  registryStatus?: string | null;
  source: string;
};

export function verifyIdentification(
  identificationTypeCode: string, number: string, verificationDigit?: number | null,
): Promise<VerifyIdentificationResult> {
  return apiFetch<VerifyIdentificationResult>("/api/v1/parties/verify-identification", {
    method: "POST",
    body: JSON.stringify({ identificationTypeCode, number, verificationDigit: verificationDigit ?? null }),
  });
}

export async function setGlobalSupplier(id: string, isGlobalSupplier: boolean): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/${encodeURIComponent(id)}/global-supplier`, {
    method: "PUT", body: JSON.stringify({ id, isGlobalSupplier }),
  });
}
