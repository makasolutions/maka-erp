import { useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Eye,
  FileText,
  FolderTree,
  Package,
  RotateCcw,
  Tags,
  Ticket,
  Trash2,
} from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { toast } from "sonner";
import {
  listTrashedBrands,
  listTrashedCategories,
  listTrashedProducts,
  restoreBrand,
  restoreCategory,
  restoreProduct,
  type PagedResponse,
} from "@/api/catalog";
import { listTrashedTickets, restoreTicket } from "@/api/tickets";
import { listTrashedFiles, restoreFile } from "@/api/files";
import { Button } from "@/components/ui/button";
import { P } from "@/auth/permissions";
import { cn } from "@/lib/cn";
import {
  EntityInitialsAvatar,
  EntityPageHeader,
} from "@/components/list";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import { describe, formatDateMono, formatRelative } from "@/lib/list-helpers";

const PAGE_SIZE = 100;

type TabKey = "products" | "brands" | "categories" | "tickets" | "files";

type RowVm = {
  id: string;
  title: string;
  subtitle: string;
  deletedByLabel: string;
  deletedRel: string;
  deletedMono: string;
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type Trashed = any;

// Per-tab wiring: list/restore endpoints, permission, restore-toast key,
// cache to invalidate on restore, and how to derive the row title/subtitle.
const TAB_CONFIG: Record<
  TabKey,
  {
    icon: React.ComponentType<{ className?: string }>;
    list: (page: number, size: number) => Promise<PagedResponse<Trashed>>;
    restore: (id: string) => Promise<unknown>;
    perm: string;
    restoredKey: string;
    invalidateKey: string[];
    title: (x: Trashed) => string;
    subtitle: (x: Trashed) => string;
  }
> = {
  products: {
    icon: Package,
    list: listTrashedProducts,
    restore: restoreProduct,
    perm: P.catalog.products.restore,
    restoredKey: "trash.restoredProduct",
    invalidateKey: ["catalog", "products"],
    title: (p) => p.name,
    subtitle: (p) => `SKU ${p.sku}`,
  },
  brands: {
    icon: Tags,
    list: listTrashedBrands,
    restore: restoreBrand,
    perm: P.catalog.brands.restore,
    restoredKey: "trash.restoredBrand",
    invalidateKey: ["catalog", "brands"],
    title: (b) => b.name,
    subtitle: (b) => `/${b.slug}`,
  },
  categories: {
    icon: FolderTree,
    list: listTrashedCategories,
    restore: restoreCategory,
    perm: P.catalog.categories.restore,
    restoredKey: "trash.restoredCategory",
    invalidateKey: ["catalog", "categories"],
    title: (c) => c.name,
    subtitle: (c) => `/${c.slug}`,
  },
  tickets: {
    icon: Ticket,
    list: listTrashedTickets,
    restore: restoreTicket,
    perm: P.tickets.restore,
    restoredKey: "trash.restoredTicket",
    invalidateKey: ["tickets"],
    title: (ticket) => ticket.title,
    subtitle: (ticket) => ticket.number,
  },
  files: {
    icon: FileText,
    list: listTrashedFiles,
    restore: restoreFile,
    perm: P.files.restore,
    restoredKey: "trash.restoredFile",
    invalidateKey: ["files"],
    title: (f) => f.originalFileName,
    subtitle: (f) => f.contentType,
  },
};

const TAB_ORDER: TabKey[] = ["products", "brands", "categories", "tickets", "files"];

type RestoreCtx = {
  restore: (id: string) => void;
  restoringId: string | null;
  perm: string;
  restoreLabel: string;
  restoringLabel: string;
};

// ── Cell templates (hook-free; read enriched row fields / ctxRef) ─────────
function EntityCell(row: RowVm) {
  return (
    <div className="flex min-w-0 items-center gap-3">
      <EntityInitialsAvatar name={row.title} size={32} />
      <div className="min-w-0">
        <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.title}</div>
        <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">{row.subtitle}</code>
      </div>
    </div>
  );
}
function DeletedByCell(row: RowVm) {
  return <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">{row.deletedByLabel}</code>;
}
function DeletedAtCell(row: RowVm) {
  if (!row.deletedRel) {
    return <span className="text-[12px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">—</span>;
  }
  return (
    <div className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
      <div>{row.deletedRel}</div>
      {row.deletedMono && <div className="text-[10.5px] opacity-70">{row.deletedMono}</div>}
    </div>
  );
}
function makeRestoreCell(ctxRef: React.MutableRefObject<RestoreCtx>) {
  // eslint-disable-next-line react/display-name
  return function RestoreCell(row: RowVm) {
    const ctx = ctxRef.current;
    const busy = ctx.restoringId === row.id;
    return (
      <div className="flex items-center justify-end" onClick={(e) => e.stopPropagation()}>
        <Button
          perm={ctx.perm}
          variant="outline"
          size="sm"
          disabled={busy}
          onClick={() => ctx.restore(row.id)}
          className="gap-1.5"
        >
          <RotateCcw className={cn("size-3.5", busy && "animate-spin")} />
          {busy ? ctx.restoringLabel : ctx.restoreLabel}
        </Button>
      </div>
    );
  };
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TrashPage() {
  const { t } = useTranslation("system");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const [tab, setTab] = useState<TabKey>("products");
  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");

  const cfg = TAB_CONFIG[tab];

  const query = useQuery({
    queryKey: ["trash", tab],
    queryFn: () => cfg.list(1, PAGE_SIZE),
  });

  const restore = useMutation({
    mutationFn: (id: string) => cfg.restore(id),
    onSuccess: () => {
      toast.success(t(cfg.restoredKey));
      void queryClient.invalidateQueries({ queryKey: ["trash", tab] });
      void queryClient.invalidateQueries({ queryKey: cfg.invalidateKey });
    },
    onError: (e) => toast.error(describe(e)),
  });

  const ctxRef = useRef<RestoreCtx>({
    restore: () => {},
    restoringId: null,
    perm: cfg.perm,
    restoreLabel: "",
    restoringLabel: "",
  });
  ctxRef.current = {
    restore: (id) => restore.mutate(id),
    restoringId: restore.isPending ? (restore.variables ?? null) : null,
    perm: cfg.perm,
    restoreLabel: t("trash.restore"),
    restoringLabel: t("trash.restoring"),
  };

  const rows: RowVm[] = useMemo(() => {
    const q = search.trim().toLowerCase();
    const items = query.data?.items ?? [];
    return items
      .map<RowVm>((x: Trashed) => ({
        id: x.id,
        title: cfg.title(x),
        subtitle: cfg.subtitle(x),
        deletedByLabel: x.deletedBy ? `${String(x.deletedBy).slice(0, 8)}…` : "—",
        deletedRel: x.deletedOnUtc ? formatRelative(x.deletedOnUtc) : "",
        deletedMono: x.deletedOnUtc ? formatDateMono(x.deletedOnUtc) : "",
      }))
      .filter((r) => !q || r.title.toLowerCase().includes(q) || r.subtitle.toLowerCase().includes(q));
  }, [query.data, cfg, search]);

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "title", headerText: t("trash.cols.entity"), template: EntityCell as any, minWidth: 240 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "deletedByLabel", headerText: t("trash.cols.deletedBy"), template: DeletedByCell as any, width: 150, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "deletedRel", headerText: t("trash.cols.deletedAt"), template: DeletedAtCell as any, width: 160, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "__actions", headerText: t("trash.cols.actions"), template: makeRestoreCell(ctxRef) as any, width: 150, textAlign: "Right", headerTextAlign: "Right", allowSorting: false, allowFiltering: false },
    ],
    [t],
  );

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Trash2}
        title={t("trash.title")}
        total={query.data ? rows.length : null}
        description={t("trash.description")}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {tc("gridFilters.filtersTab")}
        </Button>
      </EntityPageHeader>

      {/* Tab pills */}
      <nav aria-label={t("trash.navLabel")} className="flex flex-wrap items-center gap-2">
        {TAB_ORDER.map((key) => {
          const Icon = TAB_CONFIG[key].icon;
          const active = tab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => {
                setTab(key);
                setSearch("");
              }}
              aria-pressed={active}
              className={cn(
                "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-full border px-3 text-[12px] font-medium transition-colors duration-[var(--duration-fast)]",
                active
                  ? "border-transparent bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                  : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )}
            >
              <Icon className="size-3.5" aria-hidden />
              {t(`trash.tabs.${key}`)}
            </button>
          );
        })}
      </nav>

      <MakaGridFilters
        open={panelOpen}
        onClear={() => setSearch("")}
        filters={
          <MakaFilterField label={tc("gridFilters.search")} className="grow">
            <MakaFilterInput
              value={search}
              onChange={setSearch}
              placeholder={t("trash.searchPlaceholder")}
              ariaLabel={tc("gridFilters.search")}
              className="min-w-64"
            />
          </MakaFilterField>
        }
      />

      <MakaGridClient<RowVm>
        key={tab}
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName={`papelera-${tab}`}
        entityName={t(`trash.tabs.${tab}`).toLowerCase()}
        onClearFilters={() => setSearch("")}
      />

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <span>{describe(query.error)}</span>
        </div>
      )}
    </div>
  );
}
