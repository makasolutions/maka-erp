import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PartyKind = "Natural" | "Juridica";
export type PartyStatus = "Active" | "Inactive" | "Prospect";
export type LifecycleStage = "Lead" | "Mql" | "Sql" | "Opportunity" | "Customer" | "Inactive";
export type PartyTeamRole = "Owner" | "Member";

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
};
export type PartyContact = {
  id?: string; reference: string; contactTypeCode?: string | null; areaCode?: string | null;
  identificationTypeCode?: string | null; identificationNumber?: string | null;
  firstName?: string | null; lastName?: string | null; positionCode?: string | null; professionCode?: string | null;
  birthDate?: string | null; genderCode?: string | null; maritalStatusCode?: string | null;
  email?: string | null; phone?: string | null; cell?: string | null; isCommercial: boolean; notes?: string | null;
};
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
  addresses: PartyAddress[];
  contacts: PartyContact[];
  channels: PartyChannel[];
  team: PartyTeamMember[];
};

export type SearchPartiesParams = {
  search?: string;
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
  contacts: PartyContact[];
  channels: PartyChannel[];
  team: PartyTeamMember[];
};

export function createParty(input: PartyWriteInput): Promise<string> {
  return apiFetch<string>("/api/v1/parties", { method: "POST", body: JSON.stringify(input) });
}

export async function updateParty(id: string, input: PartyWriteInput): Promise<void> {
  // identificationType/number are immutable on update; the command omits them.
  const { identificationTypeCode: _t, identificationNumber: _n, ...rest } = input;
  void _t; void _n;
  await apiFetch<void>(`/api/v1/parties/${encodeURIComponent(id)}`, {
    method: "PUT", body: JSON.stringify({ id, ...rest }),
  });
}

export async function deleteParty(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/parties/${encodeURIComponent(id)}`, { method: "DELETE" });
}
