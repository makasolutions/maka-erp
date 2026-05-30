import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useTranslation } from "react-i18next";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, Plus, Tag } from "lucide-react";
import { toast } from "sonner";
import {
  createBrand,
  deleteBrand,
  searchBrands,
  updateBrand,
  type BrandDto,
  type CreateBrandInput,
  type UpdateBrandInput,
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
import { ImageInput } from "@/components/file/image-input";
import {
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import {
  MakaGrid,
  MakaGridFilters,
  MakaFilterField,
} from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import { describe, slugify } from "@/lib/list-helpers";
import { EntityAuditSection } from "@/components/entity-audit-section";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; brand: BrandDto }
  | { mode: "delete"; brand: BrandDto };

type BrandRow = BrandDto & { activeLabel: string; visibleLabel: string };

const PAGE_SIZES = [20, 50, 100];

// Boolean tri-state pill value: null = all, "true" / "false".
function triToBool(v: string | null): boolean | undefined {
  return v === null ? undefined : v === "true";
}

// ───────────────────────────────────────────────────────────────────────
//  Cell templates (hook-free; read enriched row fields)
// ───────────────────────────────────────────────────────────────────────

function BrandLogoCell(row: BrandRow) {
  return <BrandAvatar brand={row} size={32} />;
}
function BrandCodeCell(row: BrandRow) {
  return (
    <code className="font-mono text-[12px] font-medium text-[var(--color-foreground)]">
      {row.code}
    </code>
  );
}
function BrandNameCell(row: BrandRow) {
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
function BrandSlugCell(row: BrandRow) {
  return <code className="font-mono text-[11.5px] text-[var(--color-muted-foreground)]">{row.slug}</code>;
}
function BrandActiveCell(row: BrandRow) {
  return (
    <EntityStatusBadge tone={row.isActive ? "success" : "default"}>{row.activeLabel}</EntityStatusBadge>
  );
}
function BrandVisibleCell(row: BrandRow) {
  return (
    <EntityStatusBadge tone={row.isVisible ? "info" : "default"}>{row.visibleLabel}</EntityStatusBadge>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  KPI card — centred, formatted (es-CO grouping)
// ───────────────────────────────────────────────────────────────────────

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

export function BrandsPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<string | null>(null);
  const [visibleFilter, setVisibleFilter] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sort, setSort] = useState<{ by: string; dir: "asc" | "desc" }>({ by: "createdAtUtc", dir: "desc" });
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  useEffect(() => {
    const id = setTimeout(() => setDebouncedSearch(search.trim()), 250);
    return () => clearTimeout(id);
  }, [search]);

  const filters = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      isActive: triToBool(activeFilter),
      isVisible: triToBool(visibleFilter),
    }),
    [debouncedSearch, activeFilter, visibleFilter],
  );

  // Reset to page 1 when filters change.
  useEffect(() => setPage(1), [filters]);

  const query = useQuery({
    queryKey: ["catalog", "brands", filters, page, pageSize, sort],
    queryFn: () =>
      searchBrands({ ...filters, pageNumber: page, pageSize, sortBy: sort.by, sortDir: sort.dir }),
    placeholderData: keepPreviousData,
  });

  // KPI counts (lightweight totalCount-only queries).
  const totalQuery = useQuery({
    queryKey: ["catalog", "brands", "kpi", "total"],
    queryFn: () => searchBrands({ pageSize: 1 }),
    staleTime: 30_000,
  });
  const activeQuery = useQuery({
    queryKey: ["catalog", "brands", "kpi", "active"],
    queryFn: () => searchBrands({ pageSize: 1, isActive: true }),
    staleTime: 30_000,
  });
  const visibleQuery = useQuery({
    queryKey: ["catalog", "brands", "kpi", "visible"],
    queryFn: () => searchBrands({ pageSize: 1, isVisible: true }),
    staleTime: 30_000,
  });

  const rows: BrandRow[] = useMemo(
    () =>
      (query.data?.items ?? []).map((b) => ({
        ...b,
        activeLabel: b.isActive ? tc("status.active") : tc("status.inactive"),
        visibleLabel: b.isVisible ? t("brands.filters.visibleYes") : t("brands.filters.visibleNo"),
      })),
    [query.data, t, tc],
  );

  const sortFieldFor = (field: string): string | undefined =>
    ({ name: "name", slug: "slug", createdAtUtc: "createdAtUtc" })[field];

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "logoUrl", headerText: t("brands.fields.image"), template: BrandLogoCell as any, width: 72, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "code", headerText: t("brands.fields.code"), template: BrandCodeCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("brands.singular"), template: BrandNameCell as any, minWidth: 220 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "slug", headerText: t("brands.fields.slug"), template: BrandSlugCell as any, width: 170 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isActive", headerText: t("brands.fields.active"), template: BrandActiveCell as any, width: 110, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isVisible", headerText: t("brands.fields.visible"), template: BrandVisibleCell as any, width: 110, allowSorting: false, textAlign: "Center" },
      { field: "createdAtUtc", headerText: t("brands.fields.created"), width: 140, type: "date" },
    ],
    [t],
  );

  const resetFilters = () => {
    setSearch("");
    setActiveFilter(null);
    setVisibleFilter(null);
    setPage(1);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Tag}
        title={t("brands.title")}
        total={query.data?.totalCount ?? null}
        unit={t("brands.singular")}
        description={t("brands.description")}
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
          perm={P.catalog.brands.create}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("brands.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("brands.searchLabel")} className="grow">
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t("brands.searchPlaceholder")}
                className="h-8 w-full min-w-64"
              />
            </MakaFilterField>
            <MakaFilterField label={t("brands.fields.active")}>
              <EntityFilterPill<string | null>
                label={t("brands.fields.active")}
                value={activeFilter}
                onChange={setActiveFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: tc("status.active") },
                  { value: "false", label: tc("status.inactive") },
                ]}
              />
            </MakaFilterField>
            <MakaFilterField label={t("brands.fields.visible")}>
              <EntityFilterPill<string | null>
                label={t("brands.fields.visible")}
                value={visibleFilter}
                onChange={setVisibleFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: t("brands.filters.visibleYes") },
                  { value: "false", label: t("brands.filters.visibleNo") },
                ]}
              />
            </MakaFilterField>
          </>
        }
        kpis={
          <>
            <KpiCard label={t("brands.kpi.total")} value={totalQuery.data?.totalCount ?? 0} tone="var(--color-primary)" />
            <KpiCard label={t("brands.kpi.active")} value={activeQuery.data?.totalCount ?? 0} tone="var(--color-success)" />
            <KpiCard label={t("brands.kpi.visible")} value={visibleQuery.data?.totalCount ?? 0} tone="var(--color-info)" />
          </>
        }
      />

      <MakaGrid<BrandRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="marcas"
        entityName={t("brands.singular")}
        onRowClick={(row) => can(P.catalog.brands.update) && setEditor({ mode: "edit", brand: row })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.brands.update, delete: P.catalog.brands.delete }}
        onEdit={(row) => setEditor({ mode: "edit", brand: row })}
        onDelete={(row) => setEditor({ mode: "delete", brand: row })}
        serverPaging={{
          totalCount: query.data?.totalCount ?? 0,
          page,
          pageSize,
          pageSizes: PAGE_SIZES,
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

      <BrandEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteBrandDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Brand avatar — image if present, else initials tile.
// ───────────────────────────────────────────────────────────────────────

function BrandAvatar({ brand, size }: { brand: BrandDto; size: number }) {
  if (brand.logoUrl) {
    return (
      <span
        style={{ width: size, height: size }}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-lg",
          "bg-[var(--color-muted)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img
          src={brand.logoUrl}
          alt=""
          className="h-full w-full object-contain p-0.5"
          loading="lazy"
          referrerPolicy="no-referrer"
        />
      </span>
    );
  }
  return <EntityInitialsAvatar name={brand.name} size={size} />;
}

// ───────────────────────────────────────────────────────────────────────
//  Editor dialog
// ───────────────────────────────────────────────────────────────────────

function BrandEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const brand = state.mode === "edit" ? state.brand : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      code: brand?.code ?? "",
      name: brand?.name ?? "",
      description: brand?.description ?? "",
      logoUrl: brand?.logoUrl ?? "",
      isActive: brand?.isActive ?? true,
      isVisible: brand?.isVisible ?? true,
    }),
    [brand?.id, brand?.code, brand?.name, brand?.description, brand?.logoUrl, brand?.isActive, brand?.isVisible],
  );

  const [code, setCode] = useState(initial.code);
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [logoUrl, setLogoUrl] = useState(initial.logoUrl);
  const [isActive, setIsActive] = useState(initial.isActive);
  const [isVisible, setIsVisible] = useState(initial.isVisible);

  useEffect(() => {
    if (isOpen) {
      setCode(initial.code);
      setName(initial.name);
      setDescription(initial.description);
      setLogoUrl(initial.logoUrl);
      setIsActive(initial.isActive);
      setIsVisible(initial.isVisible);
    }
  }, [isOpen, initial]);

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);

  const createMutation = useMutation({
    mutationFn: (input: CreateBrandInput) => createBrand(input),
    onSuccess: () => {
      toast.success(tc("feedback.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateBrandInput) => updateBrand(input),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.updateFailed"), { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();
  const trimmedCode = code.trim();
  const canSubmit = !!trimmedName && !!trimmedCode;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!canSubmit) return;
    const payload = {
      code: trimmedCode,
      name: trimmedName,
      description: description.trim() || null,
      logoUrl: logoUrl.trim() || null,
      isActive,
      isVisible,
    };
    if (state.mode === "edit" && brand) {
      updateMutation.mutate({ brandId: brand.id, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{brand ? t("brands.actions.edit") : t("brands.actions.add")}</DialogTitle>
            <DialogDescription>
              {brand ? t("brands.editDesc", { name: brand.name }) : t("brands.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-[160px_1fr] gap-4">
              <Field id="brand-code" label={t("brands.fields.code")} required>
                <Input
                  id="brand-code"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  placeholder={t("brands.codePlaceholder")}
                  required
                  maxLength={32}
                />
              </Field>
              <Field id="brand-name" label={t("brands.fields.name")} required>
                <Input
                  id="brand-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("brands.namePlaceholder")}
                  autoFocus
                  required
                  maxLength={128}
                />
              </Field>
            </div>

            <Field id="brand-slug" label={t("brands.fields.slug")} hint={t("brands.slugHint")}>
              <div className="flex h-9 items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                  {slugPreview}
                </code>
              </div>
            </Field>

            <Field id="brand-description" label={t("brands.fields.description")} hint={t("brands.descriptionHint")}>
              <textarea
                id="brand-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={1024}
                className={cn(
                  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                )}
                placeholder={t("brands.descPlaceholder")}
              />
            </Field>

            <Field id="brand-logo" label={t("brands.fields.image")} hint={t("brands.logoHint")}>
              <ImageInput value={logoUrl} onChange={setLogoUrl} ownerType="Brand" ownerId={brand?.id} shape="square" />
            </Field>

            <div className="flex items-center gap-8">
              <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("brands.fields.active")} />
                {t("brands.fields.active")}
              </label>
              <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={isVisible} onCheckedChange={setIsVisible} aria-label={t("brands.fields.visible")} />
                {t("brands.fields.visible")}
              </label>
            </div>

            {brand && (
              <div className="border-t border-[var(--color-border)] pt-4">
                <EntityAuditSection entityKey={brand.id} entityName="Brand" />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                {tc("actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !canSubmit}>
              {isPending ? tc("feedback.saving") : brand ? tc("actions.saveChanges") : t("brands.actions.add")}
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

function DeleteBrandDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "delete";
  const brand = state.mode === "delete" ? state.brand : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteBrand(id),
    onSuccess: () => {
      toast.success(tc("feedback.deleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("brands.actions.delete")}</DialogTitle>
          <DialogDescription>{t("brands.deleteDesc", { name: brand?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => brand && deleteMutation.mutate(brand.id)}
            disabled={deleteMutation.isPending || !brand}
          >
            {deleteMutation.isPending ? tc("feedback.deleting") : t("brands.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
