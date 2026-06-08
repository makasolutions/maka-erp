import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

// ─── Tablas Básicas (parametrización) ───────────────────────────────────────

export type BasicTableDto = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isManageable: boolean;
  sortOrder: number;
  visibleInMenu: boolean;
  isGlobal: boolean;
  recordCount: number;
  createdAtUtc: string;
};

export type BasicRecordDto = {
  id: string;
  code: string;
  value: string;
  sortOrder: number;
  isActive: boolean;
};

export type BasicTableDetailDto = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isManageable: boolean;
  sortOrder: number;
  visibleInMenu: boolean;
  isGlobal: boolean;
  records: BasicRecordDto[];
};

/** Lightweight record used to populate dropdowns across the app. */
export type BasicRecordBrief = { code: string; value: string; sortOrder: number };

export type SearchBasicTablesParams = {
  search?: string;
  isGlobal?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
};

export function searchBasicTables(params: SearchBasicTablesParams = {}): Promise<PagedResponse<BasicTableDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.isGlobal != null) q.set("isGlobal", String(params.isGlobal));
  if (params.pageNumber) q.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) q.set("pageSize", String(params.pageSize));
  if (params.sort) q.set("sort", params.sort);
  const qs = q.toString();
  return apiFetch<PagedResponse<BasicTableDto>>(`/api/v1/lookups/tables${qs ? `?${qs}` : ""}`);
}

export function getBasicTableById(id: string): Promise<BasicTableDetailDto> {
  return apiFetch<BasicTableDetailDto>(`/api/v1/lookups/tables/${encodeURIComponent(id)}`);
}

export type CreateBasicTableInput = {
  code: string;
  name: string;
  description?: string | null;
  isManageable?: boolean;
  sortOrder?: number;
  visibleInMenu?: boolean;
  isGlobal?: boolean;
};

export function createBasicTable(input: CreateBasicTableInput): Promise<string> {
  return apiFetch<string>("/api/v1/lookups/tables", { method: "POST", body: JSON.stringify(input) });
}

export type UpdateBasicTableInput = {
  id: string;
  name: string;
  description?: string | null;
  isManageable: boolean;
  sortOrder: number;
  visibleInMenu: boolean;
};

export async function updateBasicTable(input: UpdateBasicTableInput): Promise<void> {
  await apiFetch<void>(`/api/v1/lookups/tables/${encodeURIComponent(input.id)}`, {
    method: "PUT", body: JSON.stringify(input),
  });
}

export async function deleteBasicTable(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/lookups/tables/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export type BasicRecordInput = {
  id?: string | null;
  code: string;
  value: string;
  sortOrder?: number;
  isActive?: boolean;
};

export async function upsertBasicRecords(tableId: string, records: BasicRecordInput[]): Promise<void> {
  await apiFetch<void>(`/api/v1/lookups/tables/${encodeURIComponent(tableId)}/records`, {
    method: "PUT", body: JSON.stringify({ id: tableId, records }),
  });
}

export async function deleteBasicRecord(tableId: string, recordId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/lookups/tables/${encodeURIComponent(tableId)}/records/${encodeURIComponent(recordId)}`,
    { method: "DELETE" },
  );
}

/** Active records of a basic table by its code — used by every dropdown. */
export function getBasicRecordsByCode(tableCode: string): Promise<BasicRecordBrief[]> {
  return apiFetch<BasicRecordBrief[]>(`/api/v1/lookups/records/by-code/${encodeURIComponent(tableCode)}`);
}
