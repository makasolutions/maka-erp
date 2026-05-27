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
  ChevronLeft,
  ChevronRight,
  CircleDollarSign,
  Minus,
  Package,
  PackageX,
  Pencil,
  Plus,
  Search,
  Trash2,
} from "lucide-react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  adjustProductStock,
  changeProductPrice,
  createProduct,
  deleteProduct,
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
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import {
  Combobox,
  EntityPageHeader,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatMoney,
} from "@/lib/list-helpers";
import { useHasPermission } from "@/auth/permission-guard";

const PAGE_SIZE = 25;
const LOW_STOCK = 10;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; product: ProductDto }
  | { mode: "delete"; product: ProductDto }
  | { mode: "price"; product: ProductDto }
  | { mode: "stock"; product: ProductDto };

// ───────────────────────────────────────────────────────────────────────
//  Filter row — simple inline filter chips above the table.
// ───────────────────────────────────────────────────────────────────────

function FilterRow({
  brands,
  categories,
  brandFilter,
  setBrandFilter,
  categoryFilter,
  setCategoryFilter,
  activeFilter,
  setActiveFilter,
}: {
  brands: BrandDto[];
  categories: CategoryDto[];
  brandFilter: string | null;
  setBrandFilter: (v: string | null) => void;
  categoryFilter: string | null;
  setCategoryFilter: (v: string | null) => void;
  activeFilter: boolean | null;
  setActiveFilter: (v: boolean | null) => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <div className="flex flex-wrap items-center gap-2">
      <Combobox
        label={t("products.fields.brand")}
        value={brandFilter}
        onChange={setBrandFilter}
        options={brands.map((b) => ({ value: b.id, label: b.name }))}
        variant="filter"
        searchable
        clearable
      />
      <Combobox
        label={t("products.fields.category")}
        value={categoryFilter}
        onChange={setCategoryFilter}
        options={categories.map((c) => ({ value: c.id, label: c.name }))}
        variant="filter"
        searchable
        clearable
      />
      <ActivePill value={activeFilter} onChange={setActiveFilter} />
    </div>
  );
}

function ActivePill({
  value,
  onChange,
}: {
  value: boolean | null;
  onChange: (v: boolean | null) => void;
}) {
  const { t } = useTranslation("catalog");
  const options = [
    { v: null as null | boolean, label: t("products.filters.all") },
    { v: true as null | boolean, label: t("products.filters.active") },
    { v: false as null | boolean, label: t("products.filters.hidden") },
  ];
  return (
    <div
      role="group"
      aria-label={t("products.filters.activeAriaLabel")}
      className="inline-flex h-8 items-center rounded-full border border-[var(--color-border)] bg-[var(--color-card)] p-0.5 text-[11px] font-semibold uppercase tracking-wider"
    >
      {options.map((opt) => {
        const isActive = value === opt.v;
        return (
          <button
            key={String(opt.v)}
            type="button"
            onClick={() => onChange(opt.v)}
            aria-pressed={isActive}
            className={cn(
              "h-7 cursor-pointer rounded-full px-3 transition-colors duration-[var(--duration-fast)]",
              isActive
                ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function ProductsPage() {
  const { t } = useTranslation("catalog");
  const canCreate = useHasPermission("Permissions.Catalog.Products.Create");
  const canUpdate = useHasPermission("Permissions.Catalog.Products.Update");
  const canDelete = useHasPermission("Permissions.Catalog.Products.Delete");
  const canAdjustStock = useHasPermission("Permissions.Catalog.Products.AdjustStock");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const [brandFilter, setBrandFilter] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<boolean | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 250);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [brandFilter, categoryFilter, activeFilter]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "products",
      {
        search: debouncedSearch,
        brandId: brandFilter,
        categoryId: categoryFilter,
        isActive: activeFilter,
        pageNumber: page,
        pageSize: PAGE_SIZE,
      },
    ],
    queryFn: () =>
      searchProducts({
        search: debouncedSearch || undefined,
        brandId: brandFilter,
        categoryId: categoryFilter,
        isActive: activeFilter,
        pageNumber: page,
        pageSize: PAGE_SIZE,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
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

  const data = query.data;
  const items = data?.items ?? [];

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

  const filtersApplied =
    brandFilter !== null || categoryFilter !== null || activeFilter !== null;
  const searchActive = debouncedSearch.length > 0 || filtersApplied;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Package}
        title={t("products.title")}
        total={data?.totalCount ?? null}
        unit={t("products.singular")}
        description={t("products.description")}
      >
        {canCreate && (
          <Button
            onClick={() => setEditor({ mode: "create" })}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            {t("products.actions.create")}
          </Button>
        )}
      </EntityPageHeader>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-4 top-1/2 size-[18px] -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
        <input
          type="text"
          placeholder={t("products.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className={cn(
            "h-[46px] w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
            "pl-12 pr-4 text-[14px] font-normal text-[var(--color-foreground)] outline-none",
            "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
            "shadow-xs",
            "transition-all duration-200",
            "focus:border-[oklch(from_var(--color-ring)_l_c_h_/_0.30)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]",
          )}
        />
        {search && (
          <button
            onClick={() => setSearch("")}
            className="absolute right-4 top-1/2 -translate-y-1/2 text-[11px] font-medium text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)] transition-colors hover:text-[var(--color-muted-foreground)]"
          >
            {t("common:actions.clear")}
          </button>
        )}
      </div>

      {/* Filters */}
      <FilterRow
        brands={brandsQuery.data?.items ?? []}
        categories={categoriesQuery.data?.items ?? []}
        brandFilter={brandFilter}
        setBrandFilter={setBrandFilter}
        categoryFilter={categoryFilter}
        setCategoryFilter={setCategoryFilter}
        activeFilter={activeFilter}
        setActiveFilter={setActiveFilter}
      />

      {/* Results */}
      {query.isLoading && items.length === 0 ? (
        <LoadingList />
      ) : items.length === 0 ? (
        <EmptyResults
          searchActive={searchActive}
          search={debouncedSearch}
          onCreate={canCreate ? () => setEditor({ mode: "create" }) : undefined}
          onClear={() => {
            setSearch("");
            setBrandFilter(null);
            setCategoryFilter(null);
            setActiveFilter(null);
          }}
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("products.found", { count: data?.totalCount ?? 0 })}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((product) => (
              <MobileCard
                key={product.id}
                product={product}
                brand={brandsById.get(product.brandId)}
                category={categoriesById.get(product.categoryId)}
                onEdit={canUpdate ? () => setEditor({ mode: "edit", product }) : undefined}
              />
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs md:block">
            {/* Header */}
            <div className="grid grid-cols-[1fr_120px_24px] gap-3 border-b border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.4)] px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] lg:grid-cols-[1fr_140px_110px_120px_24px]">
              <span>{t("products.singular")}</span>
              <span>{t("products.fields.sku")}</span>
              <span className="hidden lg:block">{t("products.fields.brand")}</span>
              <span className="hidden lg:block">{t("products.fields.price")}</span>
              <span />
            </div>

            {/* Rows */}
            {items.map((product, i) => (
              <DesktopRow
                key={product.id}
                product={product}
                brand={brandsById.get(product.brandId)}
                category={categoriesById.get(product.categoryId)}
                isLast={i === items.length - 1}
                onEdit={canUpdate ? () => setEditor({ mode: "edit", product }) : undefined}
                onDelete={canDelete ? () => setEditor({ mode: "delete", product }) : undefined}
                onPriceChange={canUpdate ? () => setEditor({ mode: "price", product }) : undefined}
                onStockAdjust={canAdjustStock ? () => setEditor({ mode: "stock", product }) : undefined}
              />
            ))}
          </div>

          {/* Pagination */}
          {(data?.totalPages ?? 1) > 1 && (
            <div className="mt-3 flex items-center justify-between">
              <p className="text-[11px] text-[var(--color-muted-foreground)]">
                {t("products.pageOf", { page, total: data?.totalPages })}
              </p>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  disabled={!data?.hasPrevious}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[oklch(from_var(--color-muted)_l_c_h_/_0.5)] hover:text-[var(--color-foreground)] disabled:cursor-not-allowed disabled:opacity-30"
                >
                  <ChevronLeft className="size-4" />
                </button>
                <button
                  type="button"
                  disabled={!data?.hasNext}
                  onClick={() => setPage((p) => p + 1)}
                  className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[oklch(from_var(--color-muted)_l_c_h_/_0.5)] hover:text-[var(--color-foreground)] disabled:cursor-not-allowed disabled:opacity-30"
                >
                  <ChevronRight className="size-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

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
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function MobileCard({
  product,
  brand,
  category,
  onEdit,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
  onEdit?: () => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <Link
      to={`/catalog/products/${product.id}`}
      aria-label={t("products.openProductAria", { name: product.name })}
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        "transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)] active:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.6)]",
        "outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.4)]",
      )}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <ProductImage
            imageUrl={product.thumbnailUrl}
            initial={product.name.trim().charAt(0).toUpperCase() || "·"}
            size={40}
          />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {product.name}
              </p>
              {!product.isActive && (
                <span className="inline-flex h-4 items-center rounded-full border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.20)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] px-1.5 py-0 text-[9px] font-semibold uppercase tracking-wider text-[var(--color-destructive)]">
                  {t("products.hiddenBadge")}
                </span>
              )}
            </div>
            <div className="mt-0.5 flex items-center gap-1.5">
              <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {product.sku}
              </code>
            </div>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          {onEdit && (
            <button
              type="button"
              aria-label={t("products.editAria", { name: product.name })}
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onEdit();
              }}
              className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
            >
              <Pencil className="size-3.5" />
            </button>
          )}
          <ChevronRight className="size-4 text-[var(--color-border)]" />
        </div>
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-2">
        {brand && (
          <span className="inline-flex h-5 items-center rounded-full bg-[var(--color-secondary)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-secondary-foreground)]">
            {brand.name}
          </span>
        )}
        {category && (
          <span className="text-[11px] text-[var(--color-muted-foreground)]">
            {category.name}
          </span>
        )}
        <span className="ml-auto font-display text-[13px] font-semibold tabular-nums text-[var(--color-foreground)]">
          {formatMoney(product.price.amount, product.price.currency)}
        </span>
        <StockChip stock={product.stock} />
      </div>
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function DesktopRow({
  product,
  brand,
  category,
  isLast,
  onEdit,
  onDelete,
  onPriceChange,
  onStockAdjust,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
  isLast: boolean;
  onEdit?: () => void;
  onDelete?: () => void;
  onPriceChange?: () => void;
  onStockAdjust?: () => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <div
      className={cn(
        "group grid grid-cols-[1fr_120px_24px] items-center gap-3 px-5 py-3 transition-colors duration-100",
        "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
        "lg:grid-cols-[1fr_140px_110px_120px_24px]",
        !isLast && "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]",
        !product.isActive && "opacity-75",
      )}
    >
      {/* Image + name */}
      <Link
        to={`/catalog/products/${product.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <ProductImage
          imageUrl={product.thumbnailUrl}
          initial={product.name.trim().charAt(0).toUpperCase() || "·"}
          size={36}
        />
        <span className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {product.name}
        </span>
        {!product.isActive && (
          <span className="inline-flex h-4 shrink-0 items-center rounded-full border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.20)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] px-1.5 py-0 text-[9px] font-semibold uppercase tracking-wider text-[var(--color-destructive)]">
            {t("products.hiddenBadge")}
          </span>
        )}
      </Link>

      {/* SKU */}
      <code
        title={product.sku}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {product.sku}
      </code>

      {/* Brand (lg+) */}
      <div className="hidden lg:block">
        {brand ? (
          <span className="inline-flex max-w-full items-center rounded-full bg-[var(--color-secondary)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-secondary-foreground)]">
            <span className="truncate">{brand.name}</span>
          </span>
        ) : (
          <span className="text-[12px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">
            —
          </span>
        )}
        {category && (
          <div className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
            {category.name}
          </div>
        )}
      </div>

      {/* Price + stock (lg+) */}
      <div className="hidden items-center gap-2 lg:flex">
        {onPriceChange ? (
          <button
            type="button"
            onClick={onPriceChange}
            title={t("products.changePriceRowTitle")}
            className="cursor-pointer rounded-md px-1.5 py-0.5 text-left font-display text-[14px] font-semibold tabular-nums transition-colors hover:bg-[var(--color-muted)]"
          >
            {formatMoney(product.price.amount, product.price.currency)}
          </button>
        ) : (
          <span className="px-1.5 py-0.5 font-display text-[14px] font-semibold tabular-nums">
            {formatMoney(product.price.amount, product.price.currency)}
          </span>
        )}
        <StockChip stock={product.stock} onClick={onStockAdjust} adjustTitle={t("products.adjustStockRowTitle")} />
      </div>

      {/* Trailing actions + chevron */}
      <div className="flex items-center justify-end gap-1">
        {onEdit && (
          <button
            type="button"
            aria-label={t("products.editAria", { name: product.name })}
            onClick={onEdit}
            className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
          >
            <Pencil className="size-3.5" />
          </button>
        )}
        {onDelete && (
          <button
            type="button"
            aria-label={t("products.deleteAria", { name: product.name })}
            onClick={onDelete}
            className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)] group-hover:opacity-100"
          >
            <Trash2 className="size-3.5" />
          </button>
        )}
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Empty state
// ───────────────────────────────────────────────────────────────────────

function EmptyResults({
  searchActive,
  search,
  onCreate,
  onClear,
}: {
  searchActive: boolean;
  search: string;
  onCreate?: () => void;
  onClear: () => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center">
      <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
        {searchActive ? (
          <Search className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)]" />
        ) : (
          <PackageX className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)]" />
        )}
      </div>
      <h3 className="mb-1.5 font-display text-[17px] font-semibold text-[var(--color-foreground)]">
        {searchActive ? t("products.empty.searchTitle") : t("products.empty.title")}
      </h3>
      <p className="mb-6 max-w-[320px] text-[13px] text-[var(--color-muted-foreground)]">
        {searchActive
          ? search
            ? t("products.empty.searchBody", { term: search })
            : t("products.empty.filterBody")
          : t("products.empty.addFirstBody")}
      </p>
      {searchActive ? (
        <Button variant="outline" onClick={onClear} className="h-9 rounded-lg px-4 text-[13px]">
          {t("products.empty.clearFilters")}
        </Button>
      ) : onCreate ? (
        <Button onClick={onCreate} className="h-9 rounded-lg px-4 text-[13px]">
          <Plus className="mr-1.5 size-4" />
          {t("products.actions.add")}
        </Button>
      ) : null}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Loading state
// ───────────────────────────────────────────────────────────────────────

function LoadingList() {
  return (
    <div>
      <div className="space-y-2 md:hidden">
        {Array.from({ length: 6 }).map((_, i) => (
          <div
            key={i}
            className="flex items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4"
          >
            <Skeleton className="size-10 rounded-xl" />
            <div className="flex-1 space-y-1.5">
              <Skeleton className="h-3.5 w-40" />
              <Skeleton className="h-2.5 w-28" />
            </div>
          </div>
        ))}
      </div>
      <div className="hidden overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] md:block">
        {Array.from({ length: 6 }).map((_, i) => (
          <div
            key={i}
            className={cn(
              "grid grid-cols-[1fr_140px_110px_120px_24px] gap-3 items-center px-5 py-3",
              i < 5 && "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]",
            )}
          >
            <div className="flex items-center gap-3">
              <Skeleton className="size-9 rounded-xl" />
              <Skeleton className="h-4 w-48" />
            </div>
            <Skeleton className="h-3 w-24" />
            <Skeleton className="h-5 w-16 rounded-full" />
            <Skeleton className="h-4 w-16" />
            <Skeleton className="ml-auto size-4" />
          </div>
        ))}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Stock chip
// ───────────────────────────────────────────────────────────────────────

function StockChip({
  stock,
  onClick,
  adjustTitle,
}: {
  stock: number;
  onClick?: () => void;
  adjustTitle?: string;
}) {
  const tone =
    stock === 0 ? "danger" : stock < LOW_STOCK ? "warning" : "default";
  const tones = {
    default:
      "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-surface-1)]",
    warning:
      "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.14)] text-[var(--color-warning)] hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.22)]",
    danger:
      "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.14)] text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.22)]",
  } as const;
  const Comp = onClick ? "button" : "span";
  return (
    <Comp
      onClick={onClick}
      title={adjustTitle}
      className={cn(
        "inline-flex h-6 items-center gap-1 rounded-full px-2 text-[11px] font-semibold tabular-nums transition-colors",
        onClick && "cursor-pointer",
        tones[tone],
      )}
    >
      <Package className="size-3" />
      {stock}
    </Comp>
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
      brandId: product?.brandId ?? "",
      categoryId: product?.categoryId ?? "",
      priceAmount: product?.price.amount ?? 0,
      priceCurrency: product?.price.currency ?? "USD",
      stock: product?.stock ?? 0,
      isActive: product?.isActive ?? true,
    }),
    [product],
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
    }
  }, [isOpen, initial]);

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
      });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
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

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="product-name" label={t("products.fields.name")} required>
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
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="product-brand" label={t("products.fields.brand")} required>
                <Combobox
                  id="product-brand"
                  label={t("products.fields.brand")}
                  placeholder={t("products.fields.brandPlaceholder")}
                  value={brandId || null}
                  onChange={(v) => setBrandId(v ?? "")}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))}
                  searchable
                  required
                />
              </Field>
              <Field id="product-category" label={t("products.fields.category")} required>
                <Combobox
                  id="product-category"
                  label={t("products.fields.category")}
                  placeholder={t("products.fields.categoryPlaceholder")}
                  value={categoryId || null}
                  onChange={(v) => setCategoryId(v ?? "")}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                  searchable
                  required
                />
              </Field>
            </div>

            {!product && (
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <Field id="product-price" label={t("products.fields.price")} required>
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
                <Field id="product-currency" label={t("products.fields.currency")} required>
                  <Input
                    id="product-currency"
                    value={priceCurrency}
                    onChange={(e) => setPriceCurrency(e.target.value.toUpperCase().slice(0, 3))}
                    required
                    maxLength={3}
                    className="font-mono uppercase tracking-tight"
                  />
                </Field>
                <Field id="product-stock" label={t("products.fields.stock")} required>
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
              </div>
            )}

            <Field
              id="product-description"
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

            {product && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
                <div>
                  <div className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                    {t("products.visibility")}
                  </div>
                  <div className="mt-0.5 text-[12.5px] text-[var(--color-muted-foreground)]">
                    {isActive ? t("products.visibleDesc") : t("products.hiddenVisibilityDesc")}
                  </div>
                </div>
                <Switch
                  checked={isActive}
                  onCheckedChange={setIsActive}
                  aria-label={t("products.visibility")}
                />
              </div>
            )}
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
      setAmount(String(product.price.amount));
      setCurrency(product.price.currency);
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
  const oldAmount = product?.price.amount ?? 0;
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
                  {product && formatMoney(product.price.amount, product.price.currency)}
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
