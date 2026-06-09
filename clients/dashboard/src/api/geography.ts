import { apiFetch } from "@/lib/api-client";

export type Municipality = { code: string; name: string };
export type Department = { code: string; name: string; municipalities: Municipality[] };

/** All DIVIPOLA departments with their municipalities (global reference data, cacheable). */
export function getDepartments(): Promise<Department[]> {
  return apiFetch<Department[]>("/api/v1/geography/departments");
}
