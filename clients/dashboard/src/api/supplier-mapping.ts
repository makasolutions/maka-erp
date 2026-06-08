import { apiFetch } from "@/lib/api-client";

export type GlobalBrand = { id: string; name: string; countryOfOrigin?: string | null };

export function getGlobalBrands(search?: string): Promise<GlobalBrand[]> {
  const qs = search ? `?search=${encodeURIComponent(search)}` : "";
  return apiFetch<GlobalBrand[]>(`/api/v1/catalog/suppliers/global-brands${qs}`);
}

export function getSupplierBrands(supplierId: string): Promise<string[]> {
  return apiFetch<string[]>(`/api/v1/catalog/suppliers/${supplierId}/brands`);
}
export async function setSupplierBrands(supplierId: string, brandIds: string[]): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/suppliers/${supplierId}/brands`, {
    method: "PUT", body: JSON.stringify({ supplierId, brandIds }),
  });
}

export function getSupplierCategories(supplierId: string): Promise<string[]> {
  return apiFetch<string[]>(`/api/v1/catalog/suppliers/${supplierId}/categories`);
}
export async function setSupplierCategories(supplierId: string, categoryIds: string[]): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/suppliers/${supplierId}/categories`, {
    method: "PUT", body: JSON.stringify({ supplierId, categoryIds }),
  });
}
