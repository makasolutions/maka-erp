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
import {
  AlertTriangle,
  ArrowDown,
  CircleDollarSign,
  Eye,
  Minus,
  Package,
  PackageX,
  Plus,
} from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  adjustProductStock,
  changeProductPrice,
  createBrand,
  createCategory,
  createProduct,
  deleteProduct,
  getProductStats,
  searchBrands,
  searchCategories,
  searchProducts,
  updateProduct,
  type AdjustProductStockInput,
  type BrandDto,
  type CategoryDto,
  type ChangeProductPriceInput,
  type CreateProductInput,
  type ProductDto,
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
} from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatMoney,
} from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; product: ProductDto }
  | { mode: "delete"; product: ProductDto }
  | { mode: "price"; product: ProductDto }
  | { mode: "stock"; product: ProductDto };

type ProductRow = ProductDto & {
  brandName: string;
  categoryName: string;
  priceLabel: string;
  activeLabel: string;
  visibleLabel: string;
};

function triToBool(v: string | null): boolean | undefined {
  return v === null ? undefined : v === "true";
}

// Category / Brand filter dropdowns: match the MakaGrid action button's surface
// (card bg, hairline border, accent hover) and the SKU input's footprint
// (h-8, min-w-40) so the filter row reads as one consistent set of controls.
const FILTER_COMBO_CLASS =
  "h-8 min-w-40 rounded-md border-[var(--color-border)] bg-[var(--color-card)] shadow-none " +
  "hover:border-[var(--color-border)] hover:bg-[var(--color-accent)]";

// ── Cell templates (hook-free; read enriched row fields) ──────────────────
function ProdImageCell(row: ProductRow) {
  return <ProductImage imageUrl={row.thumbnailUrl} initial={row.name.trim().charAt(0).toUpperCase() || "·"} size={32} />;
}
function ProdSkuCell(row: ProductRow) {
  // Mono is a deliberate, register-allowed convention for codes/SKUs; colour
  // and size match the rest of the row so only the glyph shape sets it apart.
  return <code className="font-mono text-[13px] text-[var(--color-foreground)]">{row.sku}</code>;
}
function ProdNameCell(row: ProductRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      {row.description && (
        <div className="truncate text-[12px] text-[var(--color-muted-foreground)]" title={row.description}>
          {row.description}
        </div>
      )}
    </div>
  );
}
function ProdBrandCell(row: ProductRow) {
  return <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.brandName}</span>;
}
function ProdCategoryCell(row: ProductRow) {
  return <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.categoryName}</span>;
}
function ProdPriceCell(row: ProductRow) {
  // Same weight/size/colour as every other data cell — only tabular-nums (digit
  // alignment) sets it apart. No display font, no bold (product register bans
  // display fonts in data; bold made the column read as a different typeface).
  return <span className="text-[13px] tabular-nums text-[var(--color-foreground)]">{row.priceLabel}</span>;
}
function ProdActiveCell(row: ProductRow) {
  return <EntityStatusBadge tone={row.isActive ? "success" : "default"}>{row.activeLabel}</EntityStatusBadge>;
}
function ProdVisibleCell(row: ProductRow) {
  return <EntityStatusBadge tone={row.isVisible ? "info" : "default"}>{row.visibleLabel}</EntityStatusBadge>;
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

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function ProductsPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const [panelOpen, setPanelOpen] = useState(true);
  const [skuFilter, setSkuFilter] = useState("");
  const [nameFilter, setNameFilter] = useState("");
  const [debouncedSku, setDebouncedSku] = useState("");
  const [debouncedName, setDebouncedName] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [sort, setSort] = useState<{ by: string; dir: "asc" | "desc" }>({ by: "createdAtUtc", dir: "desc" });
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const [brandFilter, setBrandFilter] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<string | null>(null);
  const [visibleFilter, setVisibleFilter] = useState<string | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSku(skuFilter.trim()), 250);
    return () => clearTimeout(timer);
  }, [skuFilter]);
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedName(nameFilter.trim()), 250);
    return () => clearTimeout(timer);
  }, [nameFilter]);

  const filters = useMemo(
    () => ({
      sku: debouncedSku || undefined,
      name: debouncedName || undefined,
      brandId: brandFilter,
      categoryId: categoryFilter,
      isActive: triToBool(activeFilter),
      isVisible: triToBool(visibleFilter),
    }),
    [debouncedSku, debouncedName, brandFilter, categoryFilter, activeFilter, visibleFilter],
  );
  useEffect(() => setPage(1), [filters]);

  const query = useQuery({
    queryKey: ["catalog", "products", filters, page, pageSize, sort],
    queryFn: () =>
      searchProducts({ ...filters, pageNumber: page, pageSize, sortBy: sort.by, sortDir: sort.dir }),
    placeholderData: keepPreviousData,
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "all-for-products-filter"],
    queryFn: () => searchBrands({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "all-for-products-filter"],
    queryFn: () => searchCategories({ pageSize: 200 }),
    staleTime: 60_000,
  });

  // KPI counts — one server-side aggregate query instead of three count probes.
  const statsQuery = useQuery({
    queryKey: ["catalog", "products", "stats"],
    queryFn: getProductStats,
    staleTime: 30_000,
  });

  const data = query.data;

  const brandsById = useMemo(() => {
    const map = new Map<string, BrandDto>();
    (brandsQuery.data?.items ?? []).forEach((b) => map.set(b.id, b));
    return map;
  }, [brandsQuery.data]);

  const categoriesById = useMemo(() => {
    const map = new Map<string, CategoryDto>();
    (categoriesQuery.data?.items ?? []).forEach((c) => map.set(c.id, c));
    return map;
  }, [categoriesQuery.data]);

  const rows: ProductRow[] = useMemo(
    () =>
      (data?.items ?? []).map((p) => ({
        ...p,
        brandName: brandsById.get(p.brandId)?.name ?? "—",
        categoryName: categoriesById.get(p.categoryId ?? "")?.name ?? "—",
        priceLabel: p.price ? formatMoney(p.price.amount, p.price.currency) : "—",
        activeLabel: p.isActive ? tc("status.active") : tc("status.inactive"),
        visibleLabel: p.isVisible ? t("products.filters.visibleYes") : t("products.filters.visibleNo"),
      })),
    [data, brandsById, categoriesById, t, tc],
  );

  const sortFieldFor = (field: string): string | undefined =>
    ({ name: "name", sku: "sku", stock: "stock", priceLabel: "price", createdAtUtc: "createdAtUtc" })[field];

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "thumbnailUrl", headerText: t("products.fields.image"), template: ProdImageCell as any, width: 96, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "categoryName", headerText: t("products.fields.category"), template: ProdCategoryCell as any, width: 170, minWidth: 140, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "brandName", headerText: t("products.fields.brand"), template: ProdBrandCell as any, width: 170, minWidth: 140, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "sku", headerText: t("products.fields.sku"), template: ProdSkuCell as any, width: 140 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("products.singular"), template: ProdNameCell as any, minWidth: 220 },
      { field: "stock", headerText: t("products.fields.stock"), width: 90, textAlign: "Right" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "priceLabel", headerText: t("products.fields.price"), template: ProdPriceCell as any, width: 130, textAlign: "Right" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isActive", headerText: t("products.fields.active"), template: ProdActiveCell as any, width: 100, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isVisible", headerText: t("products.fields.visible"), template: ProdVisibleCell as any, width: 100, allowSorting: false, textAlign: "Center" },
    ],
    [t],
  );

  const resetFilters = () => {
    setSkuFilter("");
    setNameFilter("");
    setBrandFilter(null);
    setCategoryFilter(null);
    setActiveFilter(null);
    setVisibleFilter(null);
    setPage(1);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Package}
        title={t("products.title")}
        total={data?.totalCount ?? null}
        unit={t("products.singular")}
        description={t("products.description")}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {tc("gridFilters.panelToggle")}
        </Button>
        <Button
          perm={P.catalog.products.create}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("products.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("products.fields.category")}>
              <Combobox
                id="product-category-filter"
                label={t("products.fields.category")}
                placeholder={tc("status.all")}
                value={categoryFilter}
                onChange={setCategoryFilter}
                options={(categoriesQuery.data?.items ?? []).map((c) => ({ value: c.id, label: c.name }))}
                searchable
                clearable
                emptyOptionLabel={tc("status.all")}
                className={FILTER_COMBO_CLASS}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.brand")}>
              <Combobox
                id="product-brand-filter"
                label={t("products.fields.brand")}
                placeholder={tc("status.all")}
                value={brandFilter}
                onChange={setBrandFilter}
                options={(brandsQuery.data?.items ?? []).map((b) => ({ value: b.id, label: b.name }))}
                searchable
                clearable
                emptyOptionLabel={tc("status.all")}
                className={FILTER_COMBO_CLASS}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.sku")}>
              <MakaFilterInput
                value={skuFilter}
                onChange={setSkuFilter}
                placeholder={t("products.filters.skuPlaceholder")}
                ariaLabel={t("products.fields.sku")}
                className="min-w-40 font-mono"
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.singular")} className="grow">
              <MakaFilterInput
                value={nameFilter}
                onChange={setNameFilter}
                placeholder={t("products.filters.namePlaceholder")}
                ariaLabel={t("products.singular")}
                className="min-w-48"
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.active")}>
              <EntityFilterPill<string | null>
                label={t("products.fields.active")}
                value={activeFilter}
                onChange={setActiveFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: tc("status.active") },
                  { value: "false", label: tc("status.inactive") },
                ]}
              />
            </MakaFilterField>
            <MakaFilterField label={t("products.fields.visible")}>
              <EntityFilterPill<string | null>
                label={t("products.fields.visible")}
                value={visibleFilter}
                onChange={setVisibleFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: t("products.filters.visibleYes") },
                  { value: "false", label: t("products.filters.visibleNo") },
                ]}
              />
            </MakaFilterField>
          </>
        }
        kpis={
          <>
            <KpiCard label={t("products.kpi.total")} value={statsQuery.data?.total ?? 0} tone="var(--color-primary)" />
            <KpiCard label={t("products.kpi.active")} value={statsQuery.data?.active ?? 0} tone="var(--color-success)" />
            <KpiCard label={t("products.kpi.visible")} value={statsQuery.data?.visible ?? 0} tone="var(--color-info)" />
          </>
        }
      />

      <MakaGridServer<ProductRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="productos"
        entityName={t("products.singular")}
        onRowClick={(row) => can(P.catalog.products.update) && setEditor({ mode: "edit", product: row })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.products.update, delete: P.catalog.products.delete }}
        onEdit={(row) => setEditor({ mode: "edit", product: row })}
        onDelete={(row) => setEditor({ mode: "delete", product: row })}
        extraActions={[
          ...(can(P.catalog.products.update)
            ? [{ key: "price", label: t("products.actions.changePrice"), icon: CircleDollarSign, onClick: (row: ProductRow) => setEditor({ mode: "price", product: row }) }]
            : []),
          ...(can(P.catalog.products.adjustStock)
            ? [{ key: "stock", label: t("products.actions.adjustStock"), icon: PackageX, onClick: (row: ProductRow) => setEditor({ mode: "stock", product: row }) }]
            : []),
        ]}
        serverPaging={{
          totalCount: data?.totalCount ?? 0,
          page,
          pageSize,
          pageSizes: [25, 50, 100],
          onChange: ({ page: p, pageSize: ps }) => {
            setPage(p);
            setPageSize(ps);
          },
          onSortChange: (s) => {
            setPage(1);
            if (!s) setSort({ by: "createdAtUtc", dir: "desc" });
            else {
              const by = sortFieldFor(s.field);
              setSort(by ? { by, dir: s.dir } : { by: "createdAtUtc", dir: "desc" });
            }
          },
        }}
      />

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{describe(query.error)}</span>
        </div>
      )}

      <ProductEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        brands={brandsQuery.data?.items ?? []}
        categories={categoriesQuery.data?.items ?? []}
      />
      <PriceDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <StockDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}


// ───────────────────────────────────────────────────────────────────────
//  Product image
// ───────────────────────────────────────────────────────────────────────

function ProductImage({
  imageUrl,
  initial,
  size,
}: {
  imageUrl: string | null | undefined;
  initial: string;
  size: number;
}) {
  const style = { width: size, height: size };
  if (imageUrl) {
    return (
      <span
        style={style}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
          "bg-[var(--color-muted)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img
          src={imageUrl}
          alt=""
          className="h-full w-full object-cover"
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={(e) => {
            const target = e.currentTarget;
            target.style.display = "none";
            target.parentElement
              ?.querySelector<HTMLElement>("[data-fallback]")
              ?.style.removeProperty("display");
          }}
        />
        <span
          data-fallback
          style={{ display: "none" }}
          className="absolute inset-0 grid place-items-center font-display text-[14px] font-bold tracking-tight text-[var(--color-muted-foreground)]"
        >
          {initial}
        </span>
      </span>
    );
  }
  return (
    <span
      aria-hidden
      style={style}
      className={cn(
        "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
        "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)]",
        "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]",
      )}
    >
      <span className="font-display text-[12px] font-bold text-[var(--color-primary)]">
        {initial}
      </span>
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Editor dialog (create + edit)
// ───────────────────────────────────────────────────────────────────────

function ProductEditorDialog({
  state,
  onClose,
  brands,
  categories,
}: {
  state: EditorState;
  onClose: () => void;
  brands: BrandDto[];
  categories: CategoryDto[];
}) {
  const { t } = useTranslation("catalog");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const product = state.mode === "edit" ? state.product : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      sku: product?.sku ?? "",
      name: product?.name ?? "",
      description: product?.description ?? "",
      // Defaults for new products: "Genérica" brand (GEN) and "Sin categoría"
      // category (SIN-CAT) when present in this tenant.
      brandId: product?.brandId ?? brands.find((b) => b.code === "GEN")?.id ?? "",
      categoryId: product?.categoryId ?? categories.find((c) => c.code === "SIN-CAT")?.id ?? "",
      priceAmount: product?.price?.amount ?? 0,
      priceCurrency: product?.price?.currency ?? "USD",
      stock: product?.stock ?? 0,
      isActive: product?.isActive ?? true,
      isVisible: product?.isVisible ?? true,
    }),
    [product, brands, categories],
  );

  const [sku, setSku] = useState(initial.sku);
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [brandId, setBrandId] = useState(initial.brandId);
  const [categoryId, setCategoryId] = useState(initial.categoryId);
  const [priceAmount, setPriceAmount] = useState(String(initial.priceAmount));
  const [priceCurrency, setPriceCurrency] = useState(initial.priceCurrency);
  const [stock, setStock] = useState(String(initial.stock));
  const [isActive, setIsActive] = useState(initial.isActive);
  const [isVisible, setIsVisible] = useState(initial.isVisible);
  const [quickCreate, setQuickCreate] = useState<{ type: "brand" | "category"; name: string } | null>(null);

  useEffect(() => {
    if (isOpen) {
      setSku(initial.sku);
      setName(initial.name);
      setDescription(initial.description);
      setBrandId(initial.brandId);
      setCategoryId(initial.categoryId);
      setPriceAmount(String(initial.priceAmount));
      setPriceCurrency(initial.priceCurrency);
      setStock(String(initial.stock));
      setIsActive(initial.isActive);
      setIsVisible(initial.isVisible);
    }
    // Reset only when the dialog opens or the edited product changes — NOT when
    // brands/categories refetch (e.g. after inline create), which would clobber
    // a just-created selection.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, product?.id]);

  const createMutation = useMutation({
    mutationFn: (input: CreateProductInput) => createProduct(input),
    onSuccess: () => {
      toast.success(t("products.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error(t("products.createFailed"), { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProductInput) => updateProduct(input),
    onSuccess: () => {
      toast.success(t("products.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error(t("products.updateFailed"), { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();
  const trimmedSku = sku.trim();
  const validBrand = brandId !== "";
  const validCategory = categoryId !== "";
  const priceNum = Number.parseFloat(priceAmount);
  const stockNum = Number.parseInt(stock, 10);
  const valid =
    trimmedName.length > 0 &&
    (product || trimmedSku.length > 0) &&
    validBrand &&
    validCategory &&
    !Number.isNaN(priceNum) &&
    priceNum >= 0 &&
    !Number.isNaN(stockNum) &&
    stockNum >= 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    if (state.mode === "edit" && product) {
      updateMutation.mutate({
        productId: product.id,
        name: trimmedName,
        description: description.trim() || null,
        brandId,
        categoryId,
        isActive,
        isVisible,
      });
    } else {
      createMutation.mutate({
        sku: trimmedSku,
        name: trimmedName,
        description: description.trim() || null,
        brandId,
        categoryId,
        priceAmount: priceNum,
        priceCurrency,
        stock: stockNum,
        isActive,
        isVisible,
      });
    }
  };

  return (
    <>
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>
              {product ? t("products.editTitle") : t("products.createTitle")}
            </DialogTitle>
            <DialogDescription>
              {product
                ? t("products.editDetailsDesc", { name: product.name })
                : t("products.createDetailsDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="product-name" span={8} label={t("products.fields.name")} required>
                <Input
                  id="product-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("products.fields.namePlaceholder")}
                  autoFocus
                  required
                  maxLength={200}
                />
              </Field>
              <Field
                id="product-sku"
                span={4}
                label={t("products.fields.sku")}
                required={!product}
                hint={product ? t("products.fields.skuFixedHint") : t("products.fields.skuHint")}
              >
                <Input
                  id="product-sku"
                  value={sku}
                  onChange={(e) => setSku(e.target.value.toUpperCase())}
                  placeholder={t("products.fields.skuPlaceholder")}
                  required={!product}
                  disabled={!!product}
                  maxLength={64}
                  className="font-mono text-[13px] tracking-tight"
                />
              </Field>

              <Field id="product-brand" span={6} label={t("products.fields.brand")} required>
                <Combobox
                  id="product-brand"
                  label={t("products.fields.brand")}
                  placeholder={t("products.fields.brandPlaceholder")}
                  value={brandId || null}
                  onChange={(v) => setBrandId(v ?? "")}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))}
                  searchable
                  required
                  onCreate={(q) => setQuickCreate({ type: "brand", name: q })}
                  createLabel={t("products.createBrand")}
                />
              </Field>
              <Field id="product-category" span={6} label={t("products.fields.category")} required>
                <Combobox
                  id="product-category"
                  label={t("products.fields.category")}
                  placeholder={t("products.fields.categoryPlaceholder")}
                  value={categoryId || null}
                  onChange={(v) => setCategoryId(v ?? "")}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                  searchable
                  required
                  onCreate={(q) => setQuickCreate({ type: "category", name: q })}
                  createLabel={t("products.createCategory")}
                />
              </Field>

              {!product && (
                <>
                  <Field id="product-price" span={4} label={t("products.fields.price")} required>
                    <Input
                      id="product-price"
                      type="number"
                      inputMode="decimal"
                      step="0.01"
                      min="0"
                      value={priceAmount}
                      onChange={(e) => setPriceAmount(e.target.value)}
                      required
                      className="tabular-nums"
                    />
                  </Field>
                  <Field id="product-currency" span={4} label={t("products.fields.currency")} required>
                    <Input
                      id="product-currency"
                      value={priceCurrency}
                      onChange={(e) => setPriceCurrency(e.target.value.toUpperCase().slice(0, 3))}
                      required
                      maxLength={3}
                      className="font-mono uppercase tracking-tight"
                    />
                  </Field>
                  <Field id="product-stock" span={4} label={t("products.fields.stock")} required>
                    <Input
                      id="product-stock"
                      type="number"
                      inputMode="numeric"
                      step="1"
                      min="0"
                      value={stock}
                      onChange={(e) => setStock(e.target.value)}
                      required
                      className="tabular-nums"
                    />
                  </Field>
                </>
              )}

              <Field
                id="product-description"
                span={12}
                label={t("products.fields.description")}
                hint={t("products.fields.descHint")}
              >
                <textarea
                  id="product-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  maxLength={4000}
                  className={cn(
                    "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                    "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                    "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                  )}
                  placeholder={t("products.fields.descPlaceholder")}
                />
              </Field>

              <div className="col-span-1 flex items-center gap-8 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("products.fields.active")} />
                  {t("products.fields.active")}
                </label>
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isVisible} onCheckedChange={setIsVisible} aria-label={t("products.fields.visible")} />
                  {t("products.fields.visible")}
                </label>
              </div>
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !valid}>
              {isPending
                ? t("common:feedback.saving")
                : product
                  ? t("common:actions.saveChanges")
                  : t("products.actions.add")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

      {quickCreate && (
        <QuickCreateDialog
          type={quickCreate.type}
          name={quickCreate.name}
          onClose={() => setQuickCreate(null)}
          onCreated={(newId) => {
            if (quickCreate.type === "brand") setBrandId(newId);
            else setCategoryId(newId);
          }}
        />
      )}
    </>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Quick-create dialog — create a brand/category inline from the product form
//  (the combo's "+ Crear" action). Refreshes the option list (hot reload) and
//  selects the new record so the user can keep creating the product.
// ───────────────────────────────────────────────────────────────────────

function QuickCreateDialog({
  type,
  name,
  onClose,
  onCreated,
}: {
  type: "brand" | "category";
  name: string;
  onClose: () => void;
  onCreated: (id: string) => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const [n, setN] = useState(name);
  const [code, setCode] = useState(
    name.toUpperCase().replace(/[^A-Z0-9]+/g, "-").replace(/^-|-$/g, "").slice(0, 32),
  );

  const mutation = useMutation({
    mutationFn: () =>
      type === "brand"
        ? createBrand({ code: code.trim(), name: n.trim(), isActive: true, isVisible: true })
        : createCategory({ code: code.trim(), name: n.trim(), isActive: true, isVisible: true }),
    onSuccess: (newId) => {
      toast.success(tc("feedback.created"));
      queryClient.invalidateQueries({
        queryKey: ["catalog", type === "brand" ? "brands" : "categories"],
      });
      onCreated(newId);
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const canSubmit = !!n.trim() && !!code.trim();

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (canSubmit) mutation.mutate();
          }}
        >
          <DialogHeader>
            <DialogTitle>
              {type === "brand" ? t("products.createBrand") : t("products.createCategory")}
            </DialogTitle>
            <DialogDescription>{t("products.quickCreateDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="grid grid-cols-[160px_1fr] gap-4">
              <Field id="qc-code" label={t("brands.fields.code")} required>
                <Input id="qc-code" value={code} onChange={(e) => setCode(e.target.value)} required maxLength={32} />
              </Field>
              <Field id="qc-name" label={t("brands.fields.name")} required>
                <Input id="qc-name" value={n} onChange={(e) => setN(e.target.value)} required maxLength={128} autoFocus />
              </Field>
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {tc("actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !canSubmit}>
              {mutation.isPending ? tc("feedback.saving") : tc("actions.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Price dialog
// ───────────────────────────────────────────────────────────────────────

function PriceDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const { t } = useTranslation("catalog");
  const isOpen = state.mode === "price";
  const product = state.mode === "price" ? state.product : undefined;
  const queryClient = useQueryClient();

  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState("USD");

  useEffect(() => {
    if (isOpen && product) {
      setAmount(String(product.price?.amount ?? 0));
      setCurrency(product.price?.currency ?? "COP");
    }
  }, [isOpen, product]);

  const mutation = useMutation({
    mutationFn: (input: ChangeProductPriceInput) => changeProductPrice(input),
    onSuccess: () => {
      toast.success(t("products.priceChanged"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error(t("products.changePriceFailed"), { description: describe(err) }),
  });

  const newAmount = Number.parseFloat(amount);
  const valid = !Number.isNaN(newAmount) && newAmount >= 0 && currency.length === 3;
  const oldAmount = product?.price?.amount ?? 0;
  const delta = !Number.isNaN(newAmount) ? newAmount - oldAmount : 0;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid || !product) return;
            mutation.mutate({ productId: product.id, amount: newAmount, currency });
          }}
        >
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <CircleDollarSign className="size-4 text-[var(--color-primary)]" />
              {t("products.actions.changePrice")}
            </DialogTitle>
            <DialogDescription>
              {t("products.changePriceDomainDesc", { name: product?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="flex items-center justify-between rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
              <div>
                <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("products.priceWas")}
                </div>
                <div className="mt-1 font-display text-[18px] font-semibold tabular-nums">
                  {product?.price ? formatMoney(product.price.amount, product.price.currency) : "—"}
                </div>
              </div>
              <ArrowDown className="size-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-primary)]">
                  {t("products.priceBecomes")}
                </div>
                <div
                  className={cn(
                    "mt-1 font-display text-[18px] font-semibold tabular-nums",
                    delta > 0
                      ? "text-[var(--color-success)]"
                      : delta < 0
                        ? "text-[var(--color-destructive)]"
                        : "",
                  )}
                >
                  {!Number.isNaN(newAmount)
                    ? formatMoney(newAmount, currency || "USD")
                    : "—"}
                </div>
              </div>
            </div>
            <div className="grid grid-cols-[1fr_auto] gap-3">
              <Field id="price-amount" label={t("products.priceNewAmount")} required>
                <Input
                  id="price-amount"
                  type="number"
                  step="0.01"
                  min="0"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                  className="tabular-nums"
                  autoFocus
                />
              </Field>
              <Field id="price-currency" label={t("products.fields.currency")} required>
                <Input
                  id="price-currency"
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value.toUpperCase().slice(0, 3))}
                  required
                  maxLength={3}
                  className="w-20 font-mono uppercase tracking-tight"
                />
              </Field>
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? t("common:feedback.saving") : t("products.actions.changePrice")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Stock dialog
// ───────────────────────────────────────────────────────────────────────

function StockDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const { t } = useTranslation("catalog");
  const isOpen = state.mode === "stock";
  const product = state.mode === "stock" ? state.product : undefined;
  const queryClient = useQueryClient();

  const [delta, setDelta] = useState("0");

  useEffect(() => {
    if (isOpen) setDelta("0");
  }, [isOpen]);

  const mutation = useMutation({
    mutationFn: (input: AdjustProductStockInput) => adjustProductStock(input),
    onSuccess: () => {
      toast.success(t("products.stockAdjusted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error(t("products.adjustmentFailed"), { description: describe(err) }),
  });

  const deltaNum = Number.parseInt(delta, 10);
  const valid = !Number.isNaN(deltaNum) && deltaNum !== 0;
  const newStock = (product?.stock ?? 0) + (Number.isNaN(deltaNum) ? 0 : deltaNum);
  const willGoNegative = newStock < 0;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid || willGoNegative || !product) return;
            mutation.mutate({ productId: product.id, delta: deltaNum });
          }}
        >
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="size-4 text-[var(--color-primary)]" />
              {t("products.actions.adjustStock")}
            </DialogTitle>
            <DialogDescription>
              {t("products.adjustStockDomainDesc", { name: product?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="flex items-center justify-between rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3 tabular-nums">
              <div>
                <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("products.stockCurrentLabel")}
                </div>
                <div className="mt-1 font-display text-[18px] font-semibold">
                  {product?.stock ?? 0}
                </div>
              </div>
              <ArrowDown className="size-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-primary)]">
                  {t("products.priceBecomes")}
                </div>
                <div
                  className={cn(
                    "mt-1 font-display text-[18px] font-semibold",
                    willGoNegative
                      ? "text-[var(--color-destructive)]"
                      : deltaNum > 0
                        ? "text-[var(--color-success)]"
                        : deltaNum < 0
                          ? "text-[var(--color-warning)]"
                          : "",
                  )}
                >
                  {newStock}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setDelta(String((Number.parseInt(delta, 10) || 0) - 1))}
              >
                <Minus className="size-3.5" />
              </Button>
              <Input
                value={delta}
                onChange={(e) => setDelta(e.target.value)}
                type="number"
                step="1"
                className="text-center font-mono text-[15px] tabular-nums"
                aria-label={t("products.adjustDeltaAria")}
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setDelta(String((Number.parseInt(delta, 10) || 0) + 1))}
              >
                <Plus className="size-3.5" />
              </Button>
            </div>

            {willGoNegative && (
              <div className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.20)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-[12.5px] text-[var(--color-destructive)]">
                <AlertTriangle className="mt-0.5 size-3.5 shrink-0" />
                <span>
                  {t("products.stockNegativeWarning", { max: product?.stock ?? 0 })}
                </span>
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid || willGoNegative}>
              {mutation.isPending ? t("products.adjusting") : t("products.actions.adjustStock")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Delete confirmation
// ───────────────────────────────────────────────────────────────────────

function DeleteProductDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const { t } = useTranslation("catalog");
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
    onError: (err) => toast.error(t("products.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">
            {t("products.actions.delete")}
          </DialogTitle>
          <DialogDescription>
            {t("products.deleteFullDesc", {
              name: product?.name,
              date: product ? formatDate(product.createdAtUtc) : "",
            })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              {t("common:actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => product && deleteMutation.mutate(product.id)}
            disabled={deleteMutation.isPending || !product}
          >
            {deleteMutation.isPending ? t("common:feedback.deleting") : t("products.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
