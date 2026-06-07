import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Archive, BadgeCheck, Copy, Eye, FileText, Package, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  archiveProduct,
  createProduct,
  deleteProduct,
  getCategoryTree,
  duplicateProduct,
  getProductById,
  listTrashedProducts,
  publishProduct,
  restoreProduct,
  searchBrands,
  searchProducts,
  setProductCategories,
  updateProduct,
  type BrandDto,
  type CategoryDto,
  type CreateProductInput,
  type ProductDto,
  type ProductStatus,
  type ProductType,
  type UpdateProductInput,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  Combobox,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
  Field,
  FormGrid,
} from "@/components/list";
import {
  MakaGridServer,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
  MakaPriceRangeFilter,
  type MakaPriceRange,
} from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import { describe, formatDate, formatMoney, slugify, toTaxIncluded } from "@/lib/list-helpers";
import { EntityAuditSection } from "@/components/entity-audit-section";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

// ── Types ───────────────────────────────────────────────────────────────────
type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; product: ProductDto }
  | { mode: "delete"; product: ProductDto }
  | { mode: "publish"; product: ProductDto }
  | { mode: "archive"; product: ProductDto }
  | { mode: "restore"; product: ProductDto };

type ProductRow = ProductDto & { typeLabel: string; statusLabel: string };

const PRODUCT_TYPES: ProductType[] = ["Simple", "Variable", "Bundle", "Service"];
const PRODUCT_STATUSES: ProductStatus[] = ["Draft", "Active", "Archived"];

type FlatCat = { id: string; name: string; depth: number };
function flattenCats(nodes: CategoryDto[], depth = 0): FlatCat[] {
  const out: FlatCat[] = [];
  for (const n of nodes) {
    out.push({ id: n.id, name: n.name, depth });
    if (n.children?.length) out.push(...flattenCats(n.children, depth + 1));
  }
  return out;
}

type CatSelection = { categoryId: string; isPrimary: boolean };

// ── Cell templates ───────────────────────────────────────────────────────────
function ProdImageCell(row: ProductRow) {
  if (row.thumbnailUrl) {
    return (
      <span className="grid h-8 w-8 shrink-0 place-items-center overflow-hidden rounded-lg bg-[var(--color-muted)] ring-1 ring-inset ring-[var(--color-border)]">
        <img src={row.thumbnailUrl} alt="" className="h-full w-full object-cover" loading="lazy" referrerPolicy="no-referrer" />
      </span>
    );
  }
  return <EntityInitialsAvatar name={row.name} size={32} />;
}
function ProdNameCell(row: ProductRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      {row.shortDescription && (
        <div className="truncate text-[12px] text-[var(--color-muted-foreground)]">{row.shortDescription}</div>
      )}
    </div>
  );
}
function ProdBrandCell(row: ProductRow) {
  return <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.brandName ?? "—"}</span>;
}
function ProdCategoryCell(row: ProductRow) {
  if (!row.primaryCategoryName) return <span className="text-[var(--color-muted-foreground)]">—</span>;
  return (
    <span className="truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 text-[12px] text-[var(--color-foreground)]">
      {row.primaryCategoryName}
    </span>
  );
}
function ProdTypeCell(row: ProductRow) {
  return <EntityStatusBadge tone="info">{row.typeLabel}</EntityStatusBadge>;
}
function ProdStatusCell(row: ProductRow) {
  const tone = row.status === "Active" ? "success" : row.status === "Draft" ? "default" : "warning";
  return <EntityStatusBadge tone={tone}>{row.statusLabel}</EntityStatusBadge>;
}
function ProdCodesCell(row: ProductRow) {
  // Default-variation SKU plus any extra product codes (EAN/UPC/…), as a list.
  const extras = (row.codes ?? []).filter((c) => c.codeType !== "SKU");
  if (!row.defaultSku && extras.length === 0) return <span className="text-[var(--color-muted-foreground)]">—</span>;
  return (
    <div className="flex flex-col gap-0.5 py-0.5">
      {row.defaultSku && (
        <span className="inline-flex items-center gap-1 text-[10.5px]">
          <span className="w-16 shrink-0 font-semibold text-[var(--color-muted-foreground)]">SKU</span>
          <code className="font-mono text-[var(--color-foreground)]">{row.defaultSku}</code>
        </span>
      )}
      {extras.map((c) => (
        <span key={`${c.codeType}-${c.code}`} className="inline-flex items-center gap-1 text-[10.5px]">
          <span className="w-16 shrink-0 truncate font-semibold text-[var(--color-muted-foreground)]" title={c.codeType}>{c.codeType}</span>
          <code className="font-mono text-[var(--color-foreground)]">{c.code}</code>
        </span>
      ))}
    </div>
  );
}

function ProdPriceCell(row: ProductRow) {
  // Variable products: show the IVA-included price range across variations.
  // Simple products: show the single default-list price (IVA included).
  const min = row.minVariationPrice ?? row.defaultPrice ?? null;
  const max = row.maxVariationPrice ?? row.defaultPrice ?? null;
  if (min == null && max == null) return <span className="text-[var(--color-muted-foreground)]">—</span>;
  const isRange = row.type === "Variable" && min != null && max != null && min !== max;
  const label = isRange
    ? `${formatMoney(toTaxIncluded(min))} – ${formatMoney(toTaxIncluded(max))}`
    : formatMoney(toTaxIncluded((min ?? max) as number));
  return (
    <div className="flex flex-col items-end gap-0 py-0.5 text-right">
      <span className="font-medium text-[var(--color-foreground)]">{label}</span>
      <span className="text-[9.5px] uppercase tracking-wide text-[var(--color-muted-foreground)]">IVA incl.</span>
    </div>
  );
}

function KpiCard({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="flex flex-col items-center rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-4 py-3 text-center">
      <div className="flex items-center justify-center gap-2">
        <span aria-hidden className="size-2 rounded-full" style={{ backgroundColor: tone }} />
        <span className="truncate text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {label}
        </span>
      </div>
      <div className="mt-1 font-display text-[26px] font-semibold leading-none tabular-nums text-[var(--color-foreground)]">
        {value.toLocaleString("es-CO")}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────────

export function ProductsPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const navigate = useNavigate();

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [debouncedName, setDebouncedName] = useState("");
  const [brandFilter, setBrandFilter] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [codeFilter, setCodeFilter] = useState("");
  const [debouncedCode, setDebouncedCode] = useState("");
  const [tagFilter, setTagFilter] = useState("");
  const [debouncedTag, setDebouncedTag] = useState("");
  const [priceRange, setPriceRange] = useState<MakaPriceRange>({ min: null, max: null });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [sort, setSort] = useState<{ by: string; dir: "asc" | "desc" }>({ by: "name", dir: "asc" });
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });
  const [trashOpen, setTrashOpen] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => { setDebouncedName(nameFilter.trim()); setPage(1); }, 250);
    return () => clearTimeout(timer);
  }, [nameFilter]);

  useEffect(() => {
    const timer = setTimeout(() => { setDebouncedCode(codeFilter.trim()); setPage(1); }, 250);
    return () => clearTimeout(timer);
  }, [codeFilter]);

  useEffect(() => {
    const timer = setTimeout(() => { setDebouncedTag(tagFilter.trim()); setPage(1); }, 250);
    return () => clearTimeout(timer);
  }, [tagFilter]);

  useEffect(() => { setPage(1); }, [priceRange]);

  const queryParams = useMemo(() => ({
    search: debouncedName || undefined,
    brandId: brandFilter ?? undefined,
    categoryId: categoryFilter ?? undefined,
    type: (typeFilter as ProductType | null) ?? undefined,
    status: (statusFilter as ProductStatus | null) ?? undefined,
    code: debouncedCode || undefined,
    tag: debouncedTag || undefined,
    minPrice: priceRange.min ?? undefined,
    maxPrice: priceRange.max ?? undefined,
    pageNumber: page,
    pageSize,
    sort: sort.dir === "desc" ? `-${sort.by}` : sort.by,
  }), [debouncedName, brandFilter, categoryFilter, typeFilter, statusFilter, debouncedCode, debouncedTag, priceRange, page, pageSize, sort]);

  const query = useQuery({
    queryKey: ["catalog", "products", "list", queryParams],
    queryFn: () => searchProducts(queryParams),
    placeholderData: keepPreviousData,
  });

  const pageQueryClient = useQueryClient();
  const duplicateMutation = useMutation({
    mutationFn: (id: string) => duplicateProduct(id),
    onSuccess: (newId) => {
      toast.success(tc("feedback.created"));
      pageQueryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      navigate(`/catalog/products/${newId}`);
    },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });

  const trashQuery = useQuery({
    queryKey: ["catalog", "products", "trash"],
    queryFn: () => listTrashedProducts(1, 200),
    enabled: trashOpen,
    placeholderData: keepPreviousData,
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "list"],
    queryFn: () => searchBrands({ pageSize: 200, sort: "name" }),
    staleTime: 60_000,
  });

  const brandOptions = useMemo(() =>
    (brandsQuery.data?.items ?? []).map((b: BrandDto) => ({ value: b.id, label: b.name })),
    [brandsQuery.data],
  );

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: getCategoryTree,
    staleTime: 60_000,
  });

  const categoryOptions = useMemo(() =>
    flattenCats(categoriesQuery.data ?? []).map(c => ({
      value: c.id,
      label: `${"— ".repeat(c.depth)}${c.name}`,
    })),
    [categoriesQuery.data],
  );

  const allItems = query.data?.items ?? [];
  const kpiDraft = allItems.filter(p => p.status === "Draft").length;
  const kpiActive = allItems.filter(p => p.status === "Active").length;
  const kpiTrashed = trashQuery.data?.totalCount ?? 0;

  const rows: ProductRow[] = useMemo(() =>
    allItems.map(p => ({
      ...p,
      typeLabel: t(`products.types.${p.type}`, p.type),
      statusLabel: t(`products.statuses.${p.status}`, p.status),
    })),
    [allItems, t],
  );

  const trashedRows: ProductRow[] = useMemo(() =>
    (trashQuery.data?.items ?? []).map(p => ({
      ...p,
      typeLabel: t(`products.types.${p.type}`, p.type),
      statusLabel: t(`products.statuses.${p.status}`, p.status),
    })),
    [trashQuery.data, t],
  );

  const columns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "thumbnailUrl", headerText: t("products.fields.image"), template: ProdImageCell as any, width: 72, allowSorting: false, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "name", headerText: t("products.singular"), template: ProdNameCell as any, minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "brandName", headerText: t("products.fields.brand"), template: ProdBrandCell as any, width: 140 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "primaryCategoryName", headerText: t("products.fields.category"), template: ProdCategoryCell as any, width: 150 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "type", headerText: t("products.fields.type"), template: ProdTypeCell as any, width: 110, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "status", headerText: t("products.fields.status"), template: ProdStatusCell as any, width: 110, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "defaultPrice", headerText: t("products.fields.defaultPrice"), template: ProdPriceCell as any, width: 160, textAlign: "Right" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "codes", headerText: t("products.fields.codes"), template: ProdCodesCell as any, minWidth: 200, allowSorting: false },
    { field: "createdAtUtc", headerText: t("products.fields.created"), width: 140, type: "date" },
  ], [t]);

  const trashColumns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "name", headerText: t("products.singular"), template: ProdNameCell as any, minWidth: 180 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "type", headerText: t("products.fields.type"), template: ProdTypeCell as any, width: 110, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "status", headerText: t("products.fields.status"), template: ProdStatusCell as any, width: 110, textAlign: "Center" },
  ], [t]);

  const typeOptions = useMemo(() =>
    PRODUCT_TYPES.map(type => ({ value: type, label: t(`products.types.${type}`, type) })),
    [t],
  );

  const statusOptions = useMemo(() =>
    PRODUCT_STATUSES.map(status => ({ value: status, label: t(`products.statuses.${status}`, status) })),
    [t],
  );

  const resetFilters = () => {
    setNameFilter(""); setDebouncedName(""); setBrandFilter(null); setCategoryFilter(null);
    setTypeFilter(null); setStatusFilter(null); setCodeFilter(""); setDebouncedCode("");
    setTagFilter(""); setDebouncedTag("");
    setPriceRange({ min: null, max: null }); setPage(1);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Package}
        title={t("products.title")}
        total={query.data?.totalCount ?? null}
        unit={t("products.singular")}
        description={t("products.description")}
      >
        <Button
          variant="outline"
          onClick={() => setTrashOpen(v => !v)}
          aria-pressed={trashOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Trash2 className="size-4" />
          {t("products.actions.viewTrash")}
          {kpiTrashed > 0 && (
            <span className="ml-1 rounded-full bg-[var(--color-destructive)] px-1.5 py-0.5 text-[10px] font-bold text-white">
              {kpiTrashed}
            </span>
          )}
        </Button>
        <Button
          variant="outline"
          onClick={() => setPanelOpen(v => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {tc("gridFilters.panelToggle")}
        </Button>
        <Button
          perm={P.catalog.products.create}
          onClick={() => navigate("/catalog/products/new")}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("products.actions.create")}
        </Button>
      </EntityPageHeader>

      {trashOpen && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-[var(--color-foreground)]">
              {t("products.trash.title")}
            </h3>
            <Button variant="ghost" size="sm" onClick={() => setTrashOpen(false)}>
              {tc("actions.close")}
            </Button>
          </div>
          <MakaGridServer<ProductRow>
            dataSource={trashedRows}
            columns={trashColumns}
            isLoading={trashQuery.isFetching}
            serverPaging={{
              totalCount: trashQuery.data?.totalCount ?? 0,
              page: 1,
              pageSize: 200,
              onChange: () => {},
            }}
            fileName="productos-papelera"
            entityName={t("products.singular")}
            permissions={{ edit: P.catalog.products.restore }}
            onEdit={row => setEditor({ mode: "restore", product: row })}
          />
        </div>
      )}

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("products.singular")} className="grow">
              <MakaFilterInput
                value={nameFilter}
                onChange={setNameFilter}
                placeholder={t("products.filters.namePlaceholder")}
                ariaLabel={t("products.singular")}
                className="min-w-48"
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.codeFilter")}>
              <MakaFilterInput
                value={codeFilter}
                onChange={setCodeFilter}
                placeholder={t("products.filters.codePlaceholder")}
                ariaLabel={t("products.fields.codeFilter")}
                className="min-w-44"
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.tagFilter")}>
              <MakaFilterInput
                value={tagFilter}
                onChange={setTagFilter}
                placeholder={t("products.filters.tagPlaceholder")}
                ariaLabel={t("products.fields.tagFilter")}
                className="min-w-40"
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.defaultPrice")}>
              <MakaPriceRangeFilter value={priceRange} onChange={setPriceRange} />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.brand")}>
              <Combobox
                id="brand-filter"
                label={t("products.fields.brand")}
                placeholder={t("products.allBrands")}
                value={brandFilter}
                onChange={v => { setBrandFilter(v); setPage(1); }}
                options={brandOptions}
                searchable
                clearable
                emptyOptionLabel={t("products.allBrands")}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.category")}>
              <Combobox
                id="category-filter"
                label={t("products.fields.category")}
                placeholder={t("products.allCategories")}
                value={categoryFilter}
                onChange={v => { setCategoryFilter(v); setPage(1); }}
                options={categoryOptions}
                searchable
                clearable
                emptyOptionLabel={t("products.allCategories")}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.type")}>
              <EntityFilterPill<string | null>
                label={t("products.fields.type")}
                value={typeFilter}
                onChange={v => { setTypeFilter(v); setPage(1); }}
                options={[
                  { value: null, label: tc("status.all") },
                  ...typeOptions.map(o => ({ value: o.value, label: o.label })),
                ]}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.status")}>
              <EntityFilterPill<string | null>
                label={t("products.fields.status")}
                value={statusFilter}
                onChange={v => { setStatusFilter(v); setPage(1); }}
                options={[
                  { value: null, label: tc("status.all") },
                  ...statusOptions.map(o => ({ value: o.value, label: o.label })),
                ]}
              />
            </MakaFilterField>
          </>
        }
        kpis={
          <>
            <KpiCard label={t("products.kpi.total")} value={query.data?.totalCount ?? 0} tone="var(--color-primary)" />
            <KpiCard label={t("products.kpi.draft")} value={kpiDraft} tone="var(--color-muted-foreground)" />
            <KpiCard label={t("products.kpi.active")} value={kpiActive} tone="var(--color-success)" />
            <KpiCard label={t("products.kpi.trashed")} value={kpiTrashed} tone="var(--color-destructive)" />
          </>
        }
      />

      <MakaGridServer<ProductRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        serverPaging={{
          totalCount: query.data?.totalCount ?? 0,
          page,
          pageSize,
          onChange: (next) => { setPage(next.page); setPageSize(next.pageSize); },
          onSortChange: (s) => setSort(s ? { by: s.field, dir: s.dir } : { by: "name", dir: "asc" }),
        }}
        fileName="productos"
        entityName={t("products.singular")}
        onRowClick={row => can(P.catalog.products.update) && navigate(`/catalog/products/${row.id}`)}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.products.update, delete: P.catalog.products.delete }}
        onEdit={row => navigate(`/catalog/products/${row.id}`)}
        onDelete={row => setEditor({ mode: "delete", product: row })}
        extraActions={[
          {
            key: "sheet",
            label: t("sheet.viewSheet"),
            icon: FileText,
            perm: P.catalog.products.view,
            dividerBefore: true,
            onClick: row => navigate(`/catalog/products/${row.id}/sheet`),
          },
          {
            key: "duplicate",
            label: t("products.actions.duplicate"),
            icon: Copy,
            perm: P.catalog.products.create,
            onClick: row => duplicateMutation.mutate(row.id),
          },
          {
            key: "publish",
            label: t("products.actions.publish"),
            icon: BadgeCheck,
            perm: P.catalog.products.publish,
            dividerBefore: true,
            onClick: row => setEditor({ mode: "publish", product: row }),
          },
          {
            key: "archive",
            label: t("products.actions.archive"),
            icon: Archive,
            perm: P.catalog.products.archive,
            onClick: row => setEditor({ mode: "archive", product: row }),
          },
        ]}
      />

      <ProductEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        brands={brandsQuery.data?.items ?? []}
      />
      <DeleteProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <PublishProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <ArchiveProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <RestoreProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Editor dialog — Create + Edit
// ───────────────────────────────────────────────────────────────────────────

function ProductEditorDialog({
  state,
  onClose,
  brands,
}: {
  state: EditorState;
  onClose: () => void;
  brands: BrandDto[];
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const product = state.mode === "edit" ? state.product : undefined;
  const queryClient = useQueryClient();

  const brandOptions = useMemo(() =>
    brands.map(b => ({ value: b.id, label: b.name })),
    [brands],
  );

  const typeOptions = PRODUCT_TYPES.map(type => ({
    value: type,
    label: t(`products.types.${type}`, type),
  }));

  const initial = useMemo(() => ({
    name: product?.name ?? "",
    type: (product?.type ?? "Simple") as ProductType,
    shortDescription: product?.shortDescription ?? "",
    brandId: product?.brandId ?? "",
    defaultSku: product?.defaultSku ?? "",
    isVirtual: false,
    isPublic: product?.isPublic ?? false,
  }),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  [product?.id]);

  const [name, setName] = useState(initial.name);
  const [type, setType] = useState<ProductType>(initial.type);
  const [shortDescription, setShortDescription] = useState(initial.shortDescription);
  const [brandId, setBrandId] = useState(initial.brandId);
  const [defaultSku, setDefaultSku] = useState(initial.defaultSku);
  const [isVirtual, setIsVirtual] = useState(initial.isVirtual);
  const [isPublic, setIsPublic] = useState(initial.isPublic);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setType(initial.type);
      setShortDescription(initial.shortDescription);
      setBrandId(initial.brandId);
      setDefaultSku(initial.defaultSku);
      setIsVirtual(initial.isVirtual);
      setIsPublic(initial.isPublic);
    }
  }, [isOpen, initial]);

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);
  const showDefaultSku = type === "Simple" || type === "Service";

  // Category tree for the selector + the product's current category assignments.
  const categoryTreeQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: getCategoryTree,
    staleTime: 60_000,
  });
  const flatCats = useMemo(() => flattenCats(categoryTreeQuery.data ?? []), [categoryTreeQuery.data]);

  const detailQuery = useQuery({
    queryKey: ["catalog", "products", "detail", product?.id],
    queryFn: () => getProductById(product!.id),
    enabled: isOpen && !!product,
  });

  const [selectedCats, setSelectedCats] = useState<CatSelection[]>([]);
  useEffect(() => {
    if (!isOpen) return;
    if (product && detailQuery.data) {
      setSelectedCats(detailQuery.data.categories.map(c => ({ categoryId: c.id, isPrimary: c.isPrimary })));
    } else if (!product) {
      setSelectedCats([]);
    }
  }, [isOpen, product, detailQuery.data]);

  const toggleCat = (id: string) => setSelectedCats(prev => {
    const exists = prev.find(c => c.categoryId === id);
    if (exists) {
      const next = prev.filter(c => c.categoryId !== id);
      if (exists.isPrimary && next.length > 0) next[0] = { ...next[0], isPrimary: true };
      return next;
    }
    return [...prev, { categoryId: id, isPrimary: prev.length === 0 }];
  });
  const setPrimaryCat = (id: string) =>
    setSelectedCats(prev => prev.map(c => ({ ...c, isPrimary: c.categoryId === id })));

  const saveMutation = useMutation({
    mutationFn: async () => {
      let productId: string;
      if (state.mode === "edit" && product) {
        const input: UpdateProductInput = {
          productId: product.id,
          name: name.trim(),
          shortDescription: shortDescription.trim() || null,
          brandId: brandId || null,
          isVirtual,
          isPublic,
          isDownloadable: false,
          weightUnit: "KG",
          dimensionUnit: "CM",
        };
        await updateProduct(input);
        productId = product.id;
      } else {
        const input: CreateProductInput = {
          name: name.trim(),
          type,
          shortDescription: shortDescription.trim() || null,
          brandId: brandId || null,
          defaultSku: defaultSku.trim() || null,
          isPublic,
        };
        productId = await createProduct(input);
      }
      await setProductCategories({ productId, categories: selectedCats });
      return productId;
    },
    onSuccess: () => {
      toast.success(product ? t("products.updated") : t("products.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: err =>
      toast.error(product ? t("products.updateFailed") : t("products.createFailed"), { description: describe(err) }),
  });

  const isPending = saveMutation.isPending;
  const canSubmit = !!name.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!canSubmit) return;
    saveMutation.mutate();
  };

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{product ? t("products.editTitle") : t("products.createTitle")}</DialogTitle>
            <DialogDescription>
              {product
                ? t("products.editDesc", { name: product.name })
                : t("products.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="prod-name" span={8} label={t("products.fields.name")} required>
                <Input
                  id="prod-name"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  placeholder={t("products.fields.namePlaceholder")}
                  autoFocus
                  required
                  maxLength={200}
                />
              </Field>

              {!product && (
                <Field id="prod-type" span={4} label={t("products.fields.type")} required>
                  <Combobox
                    id="prod-type"
                    label={t("products.fields.type")}
                    placeholder={t("products.typePlaceholder")}
                    value={type}
                    onChange={v => setType((v ?? "Simple") as ProductType)}
                    options={typeOptions}
                  />
                </Field>
              )}

              {product && (
                <Field id="prod-slug" span={4} label={t("products.fields.slug")} hint={t("products.slugHint")}>
                  <div className="flex h-9 items-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                    <code className="truncate font-mono text-[12.5px] text-[var(--color-foreground)]">
                      {product.slug}
                    </code>
                  </div>
                </Field>
              )}

              {!product && (
                <Field id="prod-slug-preview" span={12} label={t("products.fields.slug")} hint={t("products.slugHint")}>
                  <div className="flex h-9 items-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                    <code className="truncate font-mono text-[12.5px] text-[var(--color-foreground)]">
                      {slugPreview}
                    </code>
                  </div>
                </Field>
              )}

              <Field id="prod-brand" span={6} label={t("products.fields.brand")}>
                <Combobox
                  id="prod-brand"
                  label={t("products.fields.brand")}
                  placeholder={t("products.allBrands")}
                  value={brandId || null}
                  onChange={v => setBrandId(v ?? "")}
                  options={brandOptions}
                  searchable
                  clearable
                  emptyOptionLabel={t("products.noBrand")}
                />
              </Field>

              {showDefaultSku && !product && (
                <Field id="prod-sku" span={6} label={t("products.fields.defaultSku")} hint={t("products.defaultSkuHint")}>
                  <Input
                    id="prod-sku"
                    value={defaultSku}
                    onChange={e => setDefaultSku(e.target.value.toUpperCase())}
                    placeholder="PROD-001"
                    maxLength={64}
                    className="font-mono"
                  />
                </Field>
              )}

              <Field id="prod-shortdesc" span={12} label={t("products.fields.shortDescription")} hint={t("products.descriptionHint")}>
                <textarea
                  id="prod-shortdesc"
                  value={shortDescription}
                  onChange={e => setShortDescription(e.target.value)}
                  rows={3}
                  maxLength={500}
                  className={cn(
                    "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                    "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                    "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                  )}
                  placeholder={t("products.shortDescPlaceholder")}
                />
              </Field>

              <Field id="prod-categories" span={12} label={t("products.fields.categories")} hint={t("products.categoriesHint")}>
                <div className="max-h-52 overflow-y-auto rounded-lg border border-[var(--color-input)] bg-transparent p-2">
                  {flatCats.length === 0 ? (
                    <p className="px-2 py-3 text-[13px] text-[var(--color-muted-foreground)]">
                      {t("products.noCategories")}
                    </p>
                  ) : (
                    flatCats.map(cat => {
                      const sel = selectedCats.find(c => c.categoryId === cat.id);
                      return (
                        <div
                          key={cat.id}
                          className="flex items-center gap-2 rounded-md px-2 py-1.5 hover:bg-[var(--color-accent)]"
                          style={{ paddingLeft: `${8 + cat.depth * 16}px` }}
                        >
                          <input
                            type="checkbox"
                            id={`cat-${cat.id}`}
                            checked={!!sel}
                            onChange={() => toggleCat(cat.id)}
                            className="size-4 accent-[var(--color-primary)]"
                          />
                          <label htmlFor={`cat-${cat.id}`} className="flex-1 cursor-pointer truncate text-[13px] text-[var(--color-foreground)]">
                            {cat.name}
                          </label>
                          {sel && (
                            <button
                              type="button"
                              onClick={() => setPrimaryCat(cat.id)}
                              className={cn(
                                "rounded px-2 py-0.5 text-[11px] font-medium transition-colors",
                                sel.isPrimary
                                  ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]",
                              )}
                            >
                              {sel.isPrimary ? t("products.primaryCategory") : t("products.makePrimary")}
                            </button>
                          )}
                        </div>
                      );
                    })
                  )}
                </div>
              </Field>

              <div className="col-span-1 sm:col-span-12">
                <div className="flex flex-wrap items-center gap-x-8 gap-y-2">
                  <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                    <Switch checked={isVirtual} onCheckedChange={setIsVirtual} aria-label={t("products.fields.isVirtual")} />
                    {t("products.fields.isVirtual")}
                  </label>
                  <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                    <Switch checked={isPublic} onCheckedChange={setIsPublic} aria-label={t("products.shareDropshipLabel")} />
                    {t("products.shareDropshipLabel")}
                  </label>
                </div>
                <p className="mt-1.5 text-[12px] text-[var(--color-muted-foreground)]">{t("products.shareDropshipHint")}</p>
              </div>

              {product && (
                <div className="col-span-1 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 sm:col-span-12">
                  <div className="flex items-center gap-2 text-[13px]">
                    <span className="text-[var(--color-muted-foreground)]">{t("products.fields.status")}:</span>
                    <EntityStatusBadge tone={product.status === "Active" ? "success" : product.status === "Draft" ? "default" : "warning"}>
                      {t(`products.statuses.${product.status}`, product.status)}
                    </EntityStatusBadge>
                  </div>
                  <span className="text-[12px] text-[var(--color-muted-foreground)]">{t("products.statusHint")}</span>
                </div>
              )}

              {product && (
                <div className="col-span-1 border-t border-[var(--color-border)] pt-4 sm:col-span-12">
                  <EntityAuditSection entityKey={product.id} entityName="Product" />
                </div>
              )}
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                {tc("actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !canSubmit}>
              {isPending ? tc("feedback.saving") : product ? tc("actions.saveChanges") : t("products.actions.add")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Delete dialog
// ───────────────────────────────────────────────────────────────────────────

function DeleteProductDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "delete";
  const product = state.mode === "delete" ? state.product : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    onSuccess: () => {
      toast.success(t("products.deleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: err => toast.error(t("products.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("products.actions.delete")}</DialogTitle>
          <DialogDescription>
            {t("products.deleteFullDesc", {
              name: product?.name ?? "",
              date: product ? formatDate(product.createdAtUtc) : "",
            })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => product && deleteMutation.mutate(product.id)}
            disabled={deleteMutation.isPending || !product}
          >
            {deleteMutation.isPending ? tc("feedback.deleting") : t("products.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Publish dialog
// ───────────────────────────────────────────────────────────────────────────

function PublishProductDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "publish";
  const product = state.mode === "publish" ? state.product : undefined;
  const queryClient = useQueryClient();

  const publishMutation = useMutation({
    mutationFn: (id: string) => publishProduct(id),
    onSuccess: () => {
      toast.success(t("products.published"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: err => toast.error(t("products.publishFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("products.publishConfirmTitle", { name: product?.name ?? "" })}</DialogTitle>
          <DialogDescription>{t("products.publishConfirmDesc")}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={publishMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            onClick={() => product && publishMutation.mutate(product.id)}
            disabled={publishMutation.isPending || !product}
          >
            {publishMutation.isPending ? tc("feedback.saving") : t("products.actions.publish")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Archive dialog
// ───────────────────────────────────────────────────────────────────────────

function ArchiveProductDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "archive";
  const product = state.mode === "archive" ? state.product : undefined;
  const queryClient = useQueryClient();

  const archiveMutation = useMutation({
    mutationFn: (id: string) => archiveProduct(id),
    onSuccess: () => {
      toast.success(t("products.archived"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: err => toast.error(t("products.archiveFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("products.archiveConfirmTitle", { name: product?.name ?? "" })}</DialogTitle>
          <DialogDescription>{t("products.archiveConfirmDesc")}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={archiveMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="outline"
            onClick={() => product && archiveMutation.mutate(product.id)}
            disabled={archiveMutation.isPending || !product}
            className="text-[var(--color-warning)]"
          >
            {archiveMutation.isPending ? tc("feedback.saving") : t("products.actions.archive")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Restore dialog
// ───────────────────────────────────────────────────────────────────────────

function RestoreProductDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "restore";
  const product = state.mode === "restore" ? state.product : undefined;
  const queryClient = useQueryClient();

  const restoreMutation = useMutation({
    mutationFn: (id: string) => restoreProduct(id),
    onSuccess: () => {
      toast.success(t("products.restored"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: err => toast.error(t("products.restoreFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("products.trash.restoreTitle")}</DialogTitle>
          <DialogDescription>{t("products.trash.restoreDesc", { name: product?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={restoreMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            onClick={() => product && restoreMutation.mutate(product.id)}
            disabled={restoreMutation.isPending || !product}
          >
            {restoreMutation.isPending ? tc("feedback.saving") : t("products.trash.restoreAction")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
