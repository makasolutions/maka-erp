import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Boxes,
  FileText,
  FolderTree,
  Package,
  RotateCcw,
  Tags,
  Ticket,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import {
  listTrashedBrands,
  listTrashedCategories,
  listTrashedProducts,
  restoreBrand,
  restoreCategory,
  restoreProduct,
  type BrandDto,
  type CategoryDto,
  type PagedResponse,
  type ProductDto,
} from "@/api/catalog";
import {
  listTrashedTickets,
  restoreTicket,
  type TicketDto,
} from "@/api/tickets";
import {
  listTrashedFiles,
  restoreFile,
  type FileAssetDto,
} from "@/api/files";
import { Button } from "@/components/ui/button";
import { P } from "@/auth/permissions";
import { cn } from "@/lib/cn";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
} from "@/components/list";
import {
  describe,
  formatDateMono,
  formatRelative,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1.5fr_140px_140px_100px]";

type TabKey = "products" | "brands" | "categories" | "tickets" | "files";

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TrashPage() {
  const { t } = useTranslation("system");
  const [tab, setTab] = useState<TabKey>("products");
  const [pageNumber, setPageNumber] = useState(1);

  // TABS defined inside component so labels update reactively on language change.
  const TABS: ReadonlyArray<{
    key: TabKey;
    label: string;
    icon: React.ComponentType<{ className?: string }>;
  }> = [
    { key: "products", label: t("trash.tabs.products"), icon: Package },
    { key: "brands", label: t("trash.tabs.brands"), icon: Tags },
    { key: "categories", label: t("trash.tabs.categories"), icon: FolderTree },
    { key: "tickets", label: t("trash.tabs.tickets"), icon: Ticket },
    { key: "files", label: t("trash.tabs.files"), icon: FileText },
  ];

  // Reset paging when switching tabs.
  const onTab = (next: TabKey) => {
    setTab(next);
    setPageNumber(1);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Trash2}
        title={t("trash.title")}
        description={t("trash.description")}
      />

      {/* Tab pills */}
      <nav
        aria-label={t("trash.navLabel")}
        className="flex flex-wrap items-center gap-2"
      >
        {TABS.map(({ key, label, icon: Icon }) => {
          const active = tab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => onTab(key)}
              aria-pressed={active}
              className={cn(
                "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-full border px-3 text-[12px] font-medium transition-colors duration-[var(--duration-fast)]",
                active
                  ? "border-transparent bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                  : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )}
            >
              <Icon className="size-3.5" aria-hidden />
              {label}
            </button>
          );
        })}
      </nav>

      {/* Active panel */}
      {tab === "products" && (
        <ProductsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {tab === "brands" && (
        <BrandsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {tab === "categories" && (
        <CategoriesTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {tab === "tickets" && (
        <TicketsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {tab === "files" && (
        <FilesTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Per-resource tabs (each owns its own query + restore mutation)
// ───────────────────────────────────────────────────────────────────────

function ProductsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "products", pageNumber],
    queryFn: () => listTrashedProducts(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreProduct(id),
    onSuccess: () => {
      toast.success(t("trash.restoredProduct"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "products"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="products"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(p: ProductDto) => ({
        id: p.id,
        title: p.name,
        subtitle: `SKU ${p.sku}`,
        deletedOnUtc: p.deletedOnUtc,
        deletedBy: p.deletedBy,
        isRestoring: restore.isPending && restore.variables === p.id,
        onRestore: () => restore.mutate(p.id),
        restorePerm: P.catalog.products.restore,
      })}
    />
  );
}

function BrandsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "brands", pageNumber],
    queryFn: () => listTrashedBrands(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreBrand(id),
    onSuccess: () => {
      toast.success(t("trash.restoredBrand"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "brands"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="brands"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(b: BrandDto) => ({
        id: b.id,
        title: b.name,
        subtitle: `/${b.slug}`,
        deletedOnUtc: b.deletedOnUtc,
        deletedBy: b.deletedBy,
        isRestoring: restore.isPending && restore.variables === b.id,
        onRestore: () => restore.mutate(b.id),
        restorePerm: P.catalog.brands.restore,
      })}
    />
  );
}

function CategoriesTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "categories", pageNumber],
    queryFn: () => listTrashedCategories(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreCategory(id),
    onSuccess: () => {
      toast.success(t("trash.restoredCategory"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "categories"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="categories"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(c: CategoryDto) => ({
        id: c.id,
        title: c.name,
        subtitle: `/${c.slug}`,
        deletedOnUtc: c.deletedOnUtc,
        deletedBy: c.deletedBy,
        isRestoring: restore.isPending && restore.variables === c.id,
        onRestore: () => restore.mutate(c.id),
        restorePerm: P.catalog.categories.restore,
      })}
    />
  );
}

function TicketsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "tickets", pageNumber],
    queryFn: () => listTrashedTickets(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreTicket(id),
    onSuccess: () => {
      toast.success(t("trash.restoredTicket"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "tickets"] });
      void queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="tickets"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(ticket: TicketDto) => ({
        id: ticket.id,
        title: ticket.title,
        subtitle: ticket.number,
        deletedOnUtc: ticket.deletedOnUtc,
        deletedBy: ticket.deletedBy,
        isRestoring: restore.isPending && restore.variables === ticket.id,
        onRestore: () => restore.mutate(ticket.id),
        restorePerm: P.tickets.restore,
      })}
    />
  );
}

function FilesTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "files", pageNumber],
    queryFn: () => listTrashedFiles(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreFile(id),
    onSuccess: () => {
      toast.success(t("trash.restoredFile"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "files"] });
      void queryClient.invalidateQueries({ queryKey: ["files"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="files"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(f: FileAssetDto) => ({
        id: f.id,
        title: f.originalFileName,
        subtitle: f.contentType,
        deletedOnUtc: f.deletedOnUtc,
        deletedBy: f.deletedBy,
        isRestoring: restore.isPending && restore.variables === f.id,
        onRestore: () => restore.mutate(f.id),
        restorePerm: P.files.restore,
      })}
    />
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Shared shell — list/loading/empty/error rendering for each tab body.
// ───────────────────────────────────────────────────────────────────────

type RowVm = {
  id: string;
  title: string;
  subtitle: string;
  deletedOnUtc: string | null | undefined;
  deletedBy: string | null | undefined;
  isRestoring: boolean;
  onRestore: () => void;
  /** Permission required to show the Restore button. */
  restorePerm: string;
};

type TrashQuery<T> = {
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  data: PagedResponse<T> | undefined;
};

const TAB_PATHS: Record<TabKey, string> = {
  products: "catalog/products",
  brands: "catalog/brands",
  categories: "catalog/categories",
  tickets: "tickets",
  files: "files",
};

function TrashShell<T>({
  tabKey,
  query,
  pageNumber,
  setPageNumber,
  mapRow,
}: {
  tabKey: TabKey;
  query: TrashQuery<T>;
  pageNumber: number;
  setPageNumber: (n: number) => void;
  mapRow: (item: T) => RowVm;
}) {
  const { t } = useTranslation("system");
  const label = t(`trash.tabs.${tabKey}`);
  const items = query.data?.items ?? [];
  const total = query.data?.totalCount ?? 0;
  const rows = items.map(mapRow);

  if (query.isLoading && rows.length === 0) {
    return <EntityListLoading desktopColumns={DESKTOP_COLS} />;
  }

  if (query.isError) {
    return (
      <div
        role="alert"
        className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
      >
        <span>{describe(query.error)}</span>
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <EntityEmpty
        icon={Trash2}
        title={t("trash.empty.title", { label: label.toLowerCase() })}
        body={t("trash.empty.body", { label: label.toLowerCase() })}
        action={
          <Button
            variant="outline"
            onClick={() => {
              window.location.href = `/${TAB_PATHS[tabKey]}`;
            }}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            {t("trash.empty.backTo", { label: label.toLowerCase() })}
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
          {t(`trash.count.${tabKey}`, { count: total })}
        </p>
      </div>

      {/* Mobile cards */}
      <div className="space-y-2 md:hidden">
        {rows.map((row) => (
          <TrashMobileCard key={row.id} row={row} />
        ))}
      </div>

      {/* Desktop list */}
      <EntityListCard className="hidden md:block">
        <EntityListHeader className={DESKTOP_COLS}>
          <span>{t("trash.cols.entity")}</span>
          <span>{t("trash.cols.deletedBy")}</span>
          <span>{t("trash.cols.deletedAt")}</span>
          <span className="text-right">{t("trash.cols.actions")}</span>
        </EntityListHeader>
        {rows.map((row, i) => (
          <TrashDesktopRow key={row.id} row={row} isLast={i === rows.length - 1} />
        ))}
      </EntityListCard>

      <EntityPager
        page={query.data?.pageNumber ?? pageNumber}
        totalPages={Math.max(query.data?.totalPages ?? 1, 1)}
        hasPrev={query.data?.hasPrevious ?? false}
        hasNext={query.data?.hasNext ?? false}
        onPrev={() => setPageNumber(Math.max(1, pageNumber - 1))}
        onNext={() => setPageNumber(pageNumber + 1)}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function TrashMobileCard({ row }: { row: RowVm }) {
  const { t } = useTranslation("system");
  return (
    <div
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={row.title} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {row.title}
            </p>
            <code className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {row.subtitle}
            </code>
          </div>
        </div>
        <Button
          perm={row.restorePerm}
          variant="outline"
          size="sm"
          onClick={row.onRestore}
          disabled={row.isRestoring}
          className="shrink-0 gap-1.5"
        >
          <RotateCcw className={cn("size-3.5", row.isRestoring && "animate-spin")} />
          {row.isRestoring ? "…" : t("trash.restore")}
        </Button>
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11px] text-[var(--color-muted-foreground)]">
        <span className="tabular-nums">
          {row.deletedOnUtc ? formatRelative(row.deletedOnUtc) : "—"}
        </span>
        {row.deletedOnUtc && (
          <span className="opacity-60">({formatDateMono(row.deletedOnUtc)})</span>
        )}
        {row.deletedBy && (
          <code className="font-mono">{t("trash.byPrefix", { id: row.deletedBy.slice(0, 8) })}</code>
        )}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function TrashDesktopRow({ row, isLast }: { row: RowVm; isLast: boolean }) {
  const { t } = useTranslation("system");
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast}>
      {/* Entity */}
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={row.title} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
            {row.title}
          </div>
          <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {row.subtitle}
          </code>
        </div>
      </div>

      {/* Deleted by */}
      <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
        {row.deletedBy ? `${row.deletedBy.slice(0, 8)}…` : "—"}
      </code>

      {/* Deleted at */}
      <div className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {row.deletedOnUtc ? (
          <>
            <div>{formatRelative(row.deletedOnUtc)}</div>
            <div className="text-[10.5px] opacity-70">{formatDateMono(row.deletedOnUtc)}</div>
          </>
        ) : (
          "—"
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end">
        <Button
          perm={row.restorePerm}
          variant="outline"
          size="sm"
          onClick={row.onRestore}
          disabled={row.isRestoring}
          className="gap-1.5"
        >
          <RotateCcw className={cn("size-3.5", row.isRestoring && "animate-spin")} />
          {row.isRestoring ? t("trash.restoring") : t("trash.restore")}
        </Button>
      </div>
    </EntityListRow>
  );
}

// Suppress unused warnings for shared icons when bundling per-tab views.
void Boxes;
