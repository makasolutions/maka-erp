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

/** Adopt selected global categories into the tenant's own tree. Returns how many were created. */
export function importGlobalCategories(categoryIds: string[]): Promise<number> {
  return apiFetch<number>("/api/v1/catalog/global/import-categories", {
    method: "POST",
    body: JSON.stringify({ categoryIds }),
  });
}

// ── Smart (fuzzy) search + adoption ──────────────────────────────────────────
export type GlobalCategorySuggestion = {
  id: string;
  googleCategoryId?: number | null;
  name: string;
  fullPath?: string | null;
  score: number;
  alreadyAdopted: boolean;
};
export type GlobalBrandSuggestion = {
  id: string;
  name: string;
  country?: string | null;
  logoUrl?: string | null;
  score: number;
  alreadyAdopted: boolean;
};

export function searchGlobalCategories(q: string): Promise<GlobalCategorySuggestion[]> {
  return apiFetch<GlobalCategorySuggestion[]>(`/api/v1/catalog/global/search-categories?q=${encodeURIComponent(q)}`);
}
export function searchGlobalBrands(q: string): Promise<GlobalBrandSuggestion[]> {
  return apiFetch<GlobalBrandSuggestion[]>(`/api/v1/catalog/global/search-brands?q=${encodeURIComponent(q)}`);
}
/** Adopt a global category with optional name/slug edits. Returns the tenant category id. */
export function adoptGlobalCategory(globalCategoryId: string, name: string | null, slug: string | null): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/global/adopt-category", {
    method: "POST",
    body: JSON.stringify({ globalCategoryId, name, slug }),
  });
}

// ── Aliases (editable synonyms) ──────────────────────────────────────────────
export type CatalogAliasEntity = "Category" | "Brand";
export type CatalogAliasDto = { id: string; entityType: CatalogAliasEntity; targetId: string; targetName?: string | null; alias: string };
export function getCatalogAliases(entityType?: CatalogAliasEntity): Promise<CatalogAliasDto[]> {
  const qs = entityType ? `?entityType=${entityType}` : "";
  return apiFetch<CatalogAliasDto[]>(`/api/v1/catalog/global/aliases${qs}`);
}
export function addCatalogAlias(entityType: CatalogAliasEntity, targetId: string, alias: string): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/global/aliases", {
    method: "POST",
    body: JSON.stringify({ entityType, targetId, alias }),
  });
}
export function deleteCatalogAlias(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/catalog/global/aliases/${id}`, { method: "DELETE" });
}
