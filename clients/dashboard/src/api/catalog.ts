import { apiFetch } from "@/lib/api-client";

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

// ─── Brands ────────────────────────────────────────────────────────────
// v2 model: no `code`, no `isVisible`. Fields kept optional for compat.

export type BrandDto = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  countryOfOrigin?: string | null;
  isActive: boolean;
  wooCommerceId?: number | null;

  /** @deprecated v1 field — not returned by v2 API */
  code?: string;
  /** @deprecated v1 field — not returned by v2 API */
  isVisible?: boolean;
  /** @deprecated v1 field — not returned by v2 API */
  createdAtUtc?: string;
  /** @deprecated v1 field — not returned by v2 API */
  updatedAtUtc?: string | null;
  /** @deprecated v1 field — not returned by v2 API */
  deletedOnUtc?: string | null;
  /** @deprecated v1 field — not returned by v2 API */
  deletedBy?: string | null;
};

export type SearchBrandsParams = {
  search?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  /** @deprecated v1 param */
  isVisible?: boolean;
  /** @deprecated v1 params */
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateBrandInput = {
  name: string;
  slug?: string;
  description?: string | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  countryOfOrigin?: string | null;
  isActive: boolean;
  /** @deprecated v1 field */
  code?: string;
  /** @deprecated v1 field */
  isVisible?: boolean;
};

export type UpdateBrandInput = {
  brandId: string;
  name: string;
  slug?: string;
  description?: string | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  countryOfOrigin?: string | null;
  isActive: boolean;
  /** @deprecated v1 field */
  code?: string;
  /** @deprecated v1 field */
  isVisible?: boolean;
};

export function searchBrands(params: SearchBrandsParams = {}): Promise<PagedResponse<BrandDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sort) query.set("sort", params.sort);
  return apiFetch<PagedResponse<BrandDto>>(`/api/v1/catalog/brands?${query.toString()}`);
}

export function getBrandById(id: string): Promise<BrandDto> {
  return apiFetch<BrandDto>(`/api/v1/catalog/brands/${encodeURIComponent(id)}`);
}

export async function createBrand(input: CreateBrandInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/brands", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateBrand(input: UpdateBrandInput): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/brands/${encodeURIComponent(input.brandId)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteBrand(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/brands/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Categories ────────────────────────────────────────────────────────
// v2 model: no `code`, no `isVisible`. `parentCategoryId` → `parentId`.

export type CategoryDto = {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  wooCommerceId?: number | null;
  children: CategoryDto[];

  /** @deprecated v1 field — not returned by v2 API */
  code?: string;
  /** @deprecated v1 field — use `parentId` instead */
  parentCategoryId?: string | null;
  /** @deprecated v1 field — not returned by v2 API */
  isVisible?: boolean;
  /** @deprecated v1 field */
  createdAtUtc?: string;
  /** @deprecated v1 field */
  updatedAtUtc?: string | null;
  /** @deprecated v1 field */
  deletedOnUtc?: string | null;
  /** @deprecated v1 field */
  deletedBy?: string | null;
};

/** @deprecated v1 shape — use CategoryDto (self-referencing tree) instead */
export type CategoryTreeNodeDto = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  children: CategoryTreeNodeDto[];
};

export type SearchCategoriesParams = {
  isActive?: boolean;
  parentId?: string | null;
  /** @deprecated v1 params */
  search?: string;
  parentCategoryId?: string | null;
  isVisible?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateCategoryInput = {
  name: string;
  slug?: string;
  description?: string | null;
  imageUrl?: string | null;
  parentId?: string | null;
  isActive: boolean;
  /** @deprecated v1 fields */
  code?: string;
  parentCategoryId?: string | null;
  isVisible?: boolean;
};

export type UpdateCategoryInput = {
  categoryId: string;
  name: string;
  slug?: string;
  description?: string | null;
  imageUrl?: string | null;
  parentId?: string | null;
  isActive: boolean;
  /** @deprecated v1 fields */
  code?: string;
  parentCategoryId?: string | null;
  isVisible?: boolean;
};

/** Flattens a category tree into a flat list (depth-first). */
function flattenCategoryTree(nodes: CategoryDto[]): CategoryDto[] {
  const result: CategoryDto[] = [];
  const stack = [...nodes];
  while (stack.length > 0) {
    const node = stack.shift()!;
    result.push(node);
    if (node.children?.length) stack.unshift(...node.children);
  }
  return result;
}

/**
 * Fetches categories and returns a PagedResponse-shaped object for backward
 * compatibility with pages that expect a flat paged list.
 * The v2 backend returns a tree; we flatten it here.
 */
export async function searchCategories(
  params: SearchCategoriesParams = {},
): Promise<PagedResponse<CategoryDto>> {
  const query = new URLSearchParams();
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  const parentId = params.parentId ?? params.parentCategoryId;
  if (parentId) query.set("parentId", parentId);
  const tree = await apiFetch<CategoryDto[]>(`/api/v1/catalog/categories?${query.toString()}`);
  const flat = flattenCategoryTree(tree);
  return {
    items: flat,
    pageNumber: 1,
    pageSize: flat.length || 1,
    totalCount: flat.length,
    totalPages: 1,
    hasNext: false,
    hasPrevious: false,
  };
}

/** Returns the full category tree (v2). Same as searchCategories with no filters. */
export function getCategoryTree(): Promise<CategoryDto[]> {
  return apiFetch<CategoryDto[]>("/api/v1/catalog/categories");
}

export function getCategoryById(id: string): Promise<CategoryDto> {
  return apiFetch<CategoryDto>(`/api/v1/catalog/categories/${encodeURIComponent(id)}`);
}

export async function createCategory(input: CreateCategoryInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/categories", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateCategory(input: UpdateCategoryInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/categories/${encodeURIComponent(input.categoryId)}`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export async function deleteCategory(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/categories/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Products ──────────────────────────────────────────────────────────
// v2 model: Product has NO sku, price, or stock.
// Sku → ProductVariation. Price → PriceList. Stock → Inventory module.

/** @deprecated v1 type — kept for page compat while Fase C4/C5 rebuild products */
export type MoneyDto = {
  amount: number;
  currency: string;
};

export type ProductImageDto = {
  id: string;
  productId: string;
  url: string;
  altText: string | null;
  isPrimary: boolean;
  sortOrder: number;
};

export type ProductDto = {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string | null;
  thumbnailUrl?: string | null;
  brandId?: string | null;
  brandName?: string | null;
  type: ProductType;
  status: ProductStatus;
  isVirtual: boolean;
  isPublic: boolean;
  defaultSku?: string | null;
  primaryCategoryId?: string | null;
  primaryCategoryName?: string | null;
  wooCommerceId?: number | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;

  /** @deprecated v1 fields — kept for backward compat */
  description?: string | null;
  isActive?: boolean;
  isVisible?: boolean;
  sku?: string;
  price?: MoneyDto;
  stock?: number;
  categoryId?: string;
  images?: ProductImageDto[];
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
};

export type ProductType   = "Simple" | "Variable" | "Bundle" | "Service";
export type ProductStatus = "Draft" | "Active" | "Archived";

export type SearchProductsParams = {
  search?: string;
  brandId?: string | null;
  type?: ProductType | null;
  status?: ProductStatus | null;
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  /** @deprecated v1 params */
  isActive?: boolean | null;
  sku?: string;
  name?: string;
  categoryId?: string | null;
  isVisible?: boolean | null;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateProductInput = {
  name: string;
  slug?: string | null;
  type: ProductType;
  shortDescription?: string | null;
  brandId?: string | null;
  taxRateId?: string | null;
  shippingClassId?: string | null;
  defaultSku?: string | null;
  isPublic?: boolean;
};

export type UpdateProductInput = {
  productId: string;
  name: string;
  shortDescription?: string | null;
  description?: string | null;
  technicalSpecs?: string | null;
  brandId?: string | null;
  taxRateId?: string | null;
  shippingClassId?: string | null;
  isVirtual?: boolean;
  isDownloadable?: boolean;
  isPublic?: boolean;
  weight?: number | null;
  weightUnit?: string;
  dimensionLength?: number | null;
  dimensionWidth?: number | null;
  dimensionHeight?: number | null;
  dimensionUnit?: string;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
  specs?: string | null;
};

/** @deprecated v1 type */
export type ChangeProductPriceInput = {
  productId: string;
  amount: number;
  currency: string;
};

/** @deprecated v1 type */
export type AdjustProductStockInput = {
  productId: string;
  delta: number;
};

export function searchProducts(
  params: SearchProductsParams = {},
): Promise<PagedResponse<ProductDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.brandId) query.set("brandId", params.brandId);
  if (params.categoryId) query.set("categoryId", params.categoryId);
  if (params.type) query.set("type", params.type);
  if (params.status) query.set("status", params.status);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sort) query.set("sort", params.sort);
  return apiFetch<PagedResponse<ProductDto>>(
    `/api/v1/catalog/products?${query.toString()}`,
  );
}

export type ProductCategoryRef = {
  id: string;
  name: string;
  slug: string;
  isPrimary: boolean;
};

export type ProductDetailDto = ProductDto & {
  categories: ProductCategoryRef[];
  tags?: string[];
  // §2.6 full fields returned by GetProductById
  description?: string | null;
  technicalSpecs?: string | null;
  specs?: string | null;
  taxRateId?: string | null;
  shippingClassId?: string | null;
  isDownloadable?: boolean;
  weight?: number | null;
  weightUnit?: string;
  dimensionLength?: number | null;
  dimensionWidth?: number | null;
  dimensionHeight?: number | null;
  dimensionUnit?: string;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
};

export function getProductById(id: string): Promise<ProductDetailDto> {
  return apiFetch<ProductDetailDto>(`/api/v1/catalog/products/${encodeURIComponent(id)}`);
}

export type SetProductCategoriesInput = {
  productId: string;
  categories: { categoryId: string; isPrimary: boolean }[];
};

export async function setProductCategories(input: SetProductCategoriesInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}/categories`,
    { method: "PUT", body: JSON.stringify({ categories: input.categories }) },
  );
}

export async function createProduct(input: CreateProductInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/products", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateProduct(input: UpdateProductInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export async function publishProduct(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/products/${encodeURIComponent(id)}/publish`, {
    method: "POST",
  });
}

export async function archiveProduct(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/products/${encodeURIComponent(id)}/archive`, {
    method: "POST",
  });
}

// ─── Product images (spec §2.8) ─────────────────────────────────────────

export function getProductImages(productId: string): Promise<ProductImageDto[]> {
  return apiFetch<ProductImageDto[]>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images`,
  );
}

export function addProductImage(
  productId: string,
  input: { url: string; altText?: string | null; isPrimary?: boolean; sortOrder?: number },
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images`,
    {
      method: "POST",
      body: JSON.stringify({
        url: input.url,
        altText: input.altText ?? null,
        isPrimary: input.isPrimary ?? false,
        sortOrder: input.sortOrder ?? 0,
      }),
    },
  );
}

export async function removeProductImage(productId: string, imageId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}`,
    { method: "DELETE" },
  );
}

export function setPrimaryImage(productId: string, imageId: string): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}/primary`,
    { method: "PUT" },
  );
}

/** @deprecated v1 endpoint — price managed via PriceList in v2 */
export async function changeProductPrice(input: ChangeProductPriceInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}/price`,
    {
      method: "PATCH",
      body: JSON.stringify(input),
    },
  );
}

/** @deprecated v1 endpoint — stock managed via Inventory module in v2 */
export async function adjustProductStock(
  input: AdjustProductStockInput,
): Promise<{ stock: number }> {
  return apiFetch<{ stock: number }>(
    `/api/v1/catalog/products/${encodeURIComponent(input.productId)}/stock`,
    {
      method: "PATCH",
      body: JSON.stringify(input),
    },
  );
}

/** @todo Fase C4 */
export async function deleteProduct(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/products/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Variations (C6) ──────────────────────────────────────────────────

export type VariationDto = {
  id: string;
  productId: string;
  sku: string;
  description?: string | null;
  isDefault: boolean;
  isActive: boolean;
  isDeleted: boolean;
  weight?: number | null;
  weightUnit?: string | null;
  imageUrl?: string | null;
  manageStock: boolean;
  allowBackorders: boolean;
  soldIndividually: boolean;
  lowStockThreshold?: number | null;
  isVirtual: boolean;
  wooCommerceId?: number | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  attributeValues: VariationAttributeValueDto[];
};

export type VariationAttributeValueDto = {
  attributeId: string;
  attributeName: string;
  valueId: string;
  value: string;
  colorCode: string | null;
};

export type AddVariationInput = {
  sku: string;
  description?: string | null;
  isDefault?: boolean;
  isActive?: boolean;
  weight?: number | null;
  weightUnit?: string | null;
  imageUrl?: string | null;
  manageStock?: boolean;
  allowBackorders?: boolean;
  soldIndividually?: boolean;
  lowStockThreshold?: number | null;
  isVirtual?: boolean;
};

export type UpdateVariationInput = {
  description?: string | null;
  isActive: boolean;
  weight?: number | null;
  weightUnit?: string | null;
  imageUrl?: string | null;
  manageStock: boolean;
  allowBackorders: boolean;
  soldIndividually: boolean;
  lowStockThreshold?: number | null;
  isVirtual: boolean;
};

export function getVariations(productId: string): Promise<VariationDto[]> {
  return apiFetch<VariationDto[]>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations`,
  );
}

export async function addVariation(productId: string, input: AddVariationInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations`,
    { method: "POST", body: JSON.stringify(input) },
  );
}

export async function updateVariation(
  productId: string,
  variationId: string,
  input: UpdateVariationInput,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}`,
    { method: "PUT", body: JSON.stringify(input) },
  );
}

export async function deleteVariation(productId: string, variationId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}`,
    { method: "DELETE" },
  );
}

export function restoreVariation(productId: string, variationId: string): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}/restore`,
    { method: "POST" },
  );
}

// ─── Product Codes (C7) ───────────────────────────────────────────────

export const PRODUCT_CODE_TYPES = [
  "SKU", "EAN", "UPC", "ISBN", "GTIN", "PartNumber", "ManufacturerCode", "SupplierCode",
] as const;
export type ProductCodeType = (typeof PRODUCT_CODE_TYPES)[number];

export type ProductCodeDto = {
  id: string;
  variationId: string;
  codeType: string;
  code: string;
  isPrimary: boolean;
  supplierId?: string | null;
  createdAtUtc: string;
};

export type AddProductCodeInput = {
  codeType: string;
  code: string;
  isPrimary?: boolean;
  supplierId?: string | null;
};

export function getProductCodes(productId: string, variationId: string): Promise<ProductCodeDto[]> {
  return apiFetch<ProductCodeDto[]>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}/codes`,
  );
}

export async function addProductCode(
  productId: string,
  variationId: string,
  input: AddProductCodeInput,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}/codes`,
    { method: "POST", body: JSON.stringify(input) },
  );
}

export async function removeProductCode(
  productId: string,
  variationId: string,
  codeId: string,
): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/${encodeURIComponent(variationId)}/codes/${encodeURIComponent(codeId)}`,
    { method: "DELETE" },
  );
}

// ─── Trash + Restore ──────────────────────────────────────────────────

export function listTrashedBrands(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<BrandDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<BrandDto>>(`/api/v1/catalog/brands/trash?${q.toString()}`);
}

export function restoreBrand(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/brands/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

export function listTrashedCategories(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<CategoryDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<CategoryDto>>(
    `/api/v1/catalog/categories/trash?${q.toString()}`,
  );
}

export function restoreCategory(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/categories/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

export function listTrashedProducts(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<ProductDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<ProductDto>>(
    `/api/v1/catalog/products/trash?${q.toString()}`,
  );
}

export function restoreProduct(id: string): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/products/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

// ─── Attributes (spec §2.5) ─────────────────────────────────────────────

export type CatalogAttributeType = "Text" | "Color" | "Image" | "Select";

export const ATTRIBUTE_TYPES: CatalogAttributeType[] = ["Text", "Color", "Image", "Select"];

export type AttributeValueDto = {
  id: string;
  attributeId: string;
  value: string;
  colorCode: string | null;
  imageUrl: string | null;
  sortOrder: number;
  wooCommerceId: number | null;
};

export type AttributeDto = {
  id: string;
  name: string;
  slug: string;
  type: CatalogAttributeType;
  isVisibleOnProduct: boolean;
  isUsedForVariations: boolean;
  sortOrder: number;
  valueCount: number;
  wooCommerceId: number | null;
  categoryIds: string[];
};

export type AttributeDetailDto = Omit<AttributeDto, "valueCount"> & {
  createdAtUtc: string;
  updatedAtUtc: string | null;
  values: AttributeValueDto[];
};

export type SearchAttributesParams = {
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isUsedForVariations?: boolean;
  categoryId?: string;
};

export type CreateAttributeInput = {
  name: string;
  slug?: string | null;
  type: CatalogAttributeType;
  isVisibleOnProduct?: boolean;
  isUsedForVariations?: boolean;
  sortOrder?: number;
  categoryIds?: string[];
};

export type UpdateAttributeInput = {
  attributeId: string;
  name: string;
  type: CatalogAttributeType;
  isVisibleOnProduct: boolean;
  isUsedForVariations: boolean;
  sortOrder: number;
  categoryIds?: string[];
};

export type AttributeValueInput = {
  value: string;
  colorCode?: string | null;
  imageUrl?: string | null;
  sortOrder?: number;
};

export function searchAttributes(
  params: SearchAttributesParams = {},
): Promise<PagedResponse<AttributeDto>> {
  const qs = new URLSearchParams();
  if (params.pageNumber) qs.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  if (params.sort) qs.set("sort", params.sort);
  if (params.search) qs.set("search", params.search);
  if (params.isUsedForVariations !== undefined)
    qs.set("isUsedForVariations", String(params.isUsedForVariations));
  if (params.categoryId) qs.set("categoryId", params.categoryId);
  const q = qs.toString();
  return apiFetch<PagedResponse<AttributeDto>>(
    `/api/v1/catalog/attributes${q ? `?${q}` : ""}`,
  );
}

export function getAttributeById(id: string): Promise<AttributeDetailDto> {
  return apiFetch<AttributeDetailDto>(`/api/v1/catalog/attributes/${encodeURIComponent(id)}`);
}

export function createAttribute(input: CreateAttributeInput): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/attributes`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateAttribute(input: UpdateAttributeInput): Promise<string> {
  const { attributeId, ...body } = input;
  return apiFetch<string>(`/api/v1/catalog/attributes/${encodeURIComponent(attributeId)}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export async function deleteAttribute(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/attributes/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function addAttributeValue(attributeId: string, input: AttributeValueInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/attributes/${encodeURIComponent(attributeId)}/values`,
    { method: "POST", body: JSON.stringify(input) },
  );
}

export function updateAttributeValue(
  attributeId: string,
  valueId: string,
  input: AttributeValueInput,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/attributes/${encodeURIComponent(attributeId)}/values/${encodeURIComponent(valueId)}`,
    { method: "PUT", body: JSON.stringify(input) },
  );
}

export async function removeAttributeValue(attributeId: string, valueId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/attributes/${encodeURIComponent(attributeId)}/values/${encodeURIComponent(valueId)}`,
    { method: "DELETE" },
  );
}

// ─── Product attributes / variations generation (spec §2.12 / §5.4) ──────

export type ProductAttributeAssignmentInput = {
  attributeId: string;
  valueIds: string[];
  isUsedForVariations?: boolean;
  isVisibleOnProduct?: boolean;
  sortOrder?: number;
};

export function setProductAttributes(
  productId: string,
  attributes: ProductAttributeAssignmentInput[],
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/attributes`,
    { method: "PUT", body: JSON.stringify({ attributes }) },
  );
}

export type GenerateVariationsResult = {
  created: number;
  skipped: number;
  createdVariationIds: string[];
};

export function generateVariations(productId: string): Promise<GenerateVariationsResult> {
  return apiFetch<GenerateVariationsResult>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/variations/generate`,
    { method: "POST" },
  );
}

// ─── Product tags (spec §2.10) ──────────────────────────────────────────

export type ProductTagDto = {
  id: string;
  productId: string;
  name: string;
  color: string | null;
};

export function getProductTags(productId: string): Promise<ProductTagDto[]> {
  return apiFetch<ProductTagDto[]>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/tags`,
  );
}

export function setProductTags(
  productId: string,
  tags: { name: string; color?: string | null }[],
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/tags`,
    { method: "PUT", body: JSON.stringify({ tags }) },
  );
}

// ─── Tax rates (§2.3) & shipping classes (§2.4) ─────────────────────────

export type TaxRateDto = {
  id: string;
  name: string;
  rate: number;
  description: string | null;
  isDefault: boolean;
  isActive: boolean;
};

export function getTaxRates(activeOnly = false): Promise<TaxRateDto[]> {
  return apiFetch<TaxRateDto[]>(
    `/api/v1/catalog/tax-rates${activeOnly ? "?activeOnly=true" : ""}`,
  );
}

export type ShippingClassDto = {
  id: string;
  name: string;
  description: string | null;
};

export function getShippingClasses(): Promise<ShippingClassDto[]> {
  return apiFetch<ShippingClassDto[]>(`/api/v1/catalog/shipping-classes`);
}

/** Returns the SKU of the product's default variation (Simple/Service), if any. */
export async function getDefaultVariation(productId: string): Promise<VariationDto | null> {
  const variations = await getVariations(productId);
  return variations.find((v) => v.isDefault && !v.isDeleted) ?? null;
}

// ─── Bundle items (§2.13) ───────────────────────────────────────────────

export type BundleItemDto = {
  id: string;
  productId: string;
  itemVariationId: string;
  itemSku: string;
  quantity: number;
  discountPercent: number | null;
  discountFixed: number | null;
  isOptional: boolean;
  sortOrder: number;
};

export function getBundleItems(productId: string): Promise<BundleItemDto[]> {
  return apiFetch<BundleItemDto[]>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/bundle-items`,
  );
}

// ─── Tax rates / shipping classes CRUD (admin settings §6) ──────────────

export type CreateTaxRateInput = { name: string; rate: number; description?: string | null; isDefault?: boolean };
export type UpdateTaxRateInput = { id: string; name: string; rate: number; description?: string | null; isDefault: boolean; isActive: boolean };

export function createTaxRate(input: CreateTaxRateInput): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/tax-rates`, { method: "POST", body: JSON.stringify(input) });
}
export function updateTaxRate(input: UpdateTaxRateInput): Promise<string> {
  const { id, ...body } = input;
  return apiFetch<string>(`/api/v1/catalog/tax-rates/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) });
}
export async function deleteTaxRate(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/tax-rates/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export type CreateShippingClassInput = { name: string; description?: string | null };
export type UpdateShippingClassInput = { id: string; name: string; description?: string | null };

export function createShippingClass(input: CreateShippingClassInput): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/shipping-classes`, { method: "POST", body: JSON.stringify(input) });
}
export function updateShippingClass(input: UpdateShippingClassInput): Promise<string> {
  const { id, ...body } = input;
  return apiFetch<string>(`/api/v1/catalog/shipping-classes/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) });
}
export async function deleteShippingClass(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/shipping-classes/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// ─── Public (anonymous) product sheet — friendly shareable URL ──────────

export type PublicImageDto = { url: string; altText: string | null; isPrimary: boolean; sortOrder: number };
export type PublicCodeDto = { codeType: string; code: string };
export type PublicVariationDto = { sku: string; combination: string };
export type PublicProductDto = {
  id: string;
  name: string;
  slug: string;
  brandName: string | null;
  type: ProductType;
  shortDescription: string | null;
  description: string | null;
  technicalSpecs: string | null;
  specs: string | null;
  images: PublicImageDto[];
  codes: PublicCodeDto[];
  variations: PublicVariationDto[];
};

/**
 * Fetches the public product sheet for a tenant + slug WITHOUT authentication.
 * Raw fetch (not apiFetch) so it works for anonymous visitors: only the tenant
 * header is sent, no Authorization. Goes through the Vite /api proxy in dev.
 */
export async function getPublicProduct(tenant: string, slug: string): Promise<PublicProductDto> {
  const base = (await import("@/env")).env.apiBase || "";
  const res = await fetch(
    `${base}/api/v1/catalog/public/products/${encodeURIComponent(slug)}`,
    { headers: { tenant, Accept: "application/json" } },
  );
  if (!res.ok) throw new Error(res.status === 404 ? "not-found" : `HTTP ${res.status}`);
  return (await res.json()) as PublicProductDto;
}

// ─── Bundle item mutations (§2.13) ──────────────────────────────────────

export type AddBundleItemInput = {
  itemVariationId: string;
  quantity?: number;
  discountPercent?: number | null;
  discountFixed?: number | null;
  isOptional?: boolean;
  sortOrder?: number;
};

export function addBundleItem(productId: string, input: AddBundleItemInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/bundle-items`,
    { method: "POST", body: JSON.stringify(input) },
  );
}

export async function removeBundleItem(productId: string, itemId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/catalog/products/${encodeURIComponent(productId)}/bundle-items/${encodeURIComponent(itemId)}`,
    { method: "DELETE" },
  );
}

// ─── Price lists (RF-CAT-4) ─────────────────────────────────────────────

export type PriceListDto = {
  id: string;
  name: string;
  description?: string | null;
  customerSegment: string;
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
  itemCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type PriceListItemDto = {
  id: string;
  variationId: string;
  variationSku?: string | null;
  price: number;
  minQuantity?: number | null;
  salePrice?: number | null;
  salePriceFrom?: string | null;
  salePriceTo?: string | null;
  createdAt: string;
  updatedAtUtc?: string | null;
};

export type PriceListDetailDto = Omit<PriceListDto, "itemCount"> & {
  items: PriceListItemDto[];
};

export type EffectivePriceDto = {
  variationId: string;
  effectivePrice: number;
  listPrice: number;
  isSalePrice: boolean;
  customerSegment: string;
  priceListName: string;
  priceListId: string;
};

export type GetPriceListsParams = {
  search?: string;
  customerSegment?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
};

export function getPriceLists(params: GetPriceListsParams = {}): Promise<PagedResponse<PriceListDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.customerSegment) q.set("customerSegment", params.customerSegment);
  if (params.isActive != null) q.set("isActive", String(params.isActive));
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 50));
  if (params.sort) q.set("sort", params.sort);
  return apiFetch<PagedResponse<PriceListDto>>(`/api/v1/catalog/price-lists?${q.toString()}`);
}

export function getPriceListById(id: string): Promise<PriceListDetailDto> {
  return apiFetch<PriceListDetailDto>(`/api/v1/catalog/price-lists/${encodeURIComponent(id)}`);
}

export type CreatePriceListInput = {
  name: string;
  customerSegment: string;
  validFrom?: string | null;
  description?: string | null;
};

export function createPriceList(input: CreatePriceListInput): Promise<string> {
  return apiFetch<string>("/api/v1/catalog/price-lists", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export type AddPriceListItemInput = {
  variationId: string;
  price: number;
  minQuantity?: number | null;
  salePrice?: number | null;
  salePriceFrom?: string | null;
  salePriceTo?: string | null;
};

export function addPriceListItem(priceListId: string, input: AddPriceListItemInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/price-lists/${encodeURIComponent(priceListId)}/items`,
    { method: "POST", body: JSON.stringify({ priceListId, ...input }) },
  );
}

export type UpdatePriceListItemInput = {
  price: number;
  changeReason?: string | null;
  salePrice?: number | null;
  salePriceFrom?: string | null;
  salePriceTo?: string | null;
};

export function updatePriceListItem(
  priceListId: string, itemId: string, input: UpdatePriceListItemInput,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/catalog/price-lists/${encodeURIComponent(priceListId)}/items/${encodeURIComponent(itemId)}`,
    { method: "PUT", body: JSON.stringify({ priceListId, itemId, ...input }) },
  );
}

export function getEffectivePrice(variationId: string, segment?: string): Promise<EffectivePriceDto> {
  const q = new URLSearchParams({ variationId });
  if (segment) q.set("segment", segment);
  return apiFetch<EffectivePriceDto>(`/api/v1/catalog/prices/effective?${q.toString()}`);
}
