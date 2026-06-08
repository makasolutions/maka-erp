import { apiFetch } from "@/lib/api-client";

export type Industry = { id: string; code: string; name: string; sortOrder: number };

export type GlobalCategory = {
  id: string;
  googleCategoryId?: number | null;
  parentId?: string | null;
  name: string;
  fullPath?: string | null;
  rootGoogleCategoryId?: number | null;
};

export function getIndustries(): Promise<Industry[]> {
  return apiFetch<Industry[]>("/api/v1/catalog/global/industries");
}

export function getTenantIndustries(): Promise<string[]> {
  return apiFetch<string[]>("/api/v1/catalog/global/tenant-industries");
}

export async function setTenantIndustries(industryIds: string[]): Promise<void> {
  await apiFetch<void>("/api/v1/catalog/global/tenant-industries", {
    method: "PUT",
    body: JSON.stringify({ industryIds }),
  });
}

export function getGlobalCategories(search?: string): Promise<GlobalCategory[]> {
  const qs = search ? `?search=${encodeURIComponent(search)}` : "";
  return apiFetch<GlobalCategory[]>(`/api/v1/catalog/global/global-categories${qs}`);
}
