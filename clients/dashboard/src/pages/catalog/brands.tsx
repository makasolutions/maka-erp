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
import { Eye, Plus, Tag, Trash2 } from "lucide-react";
import { SaveIcon, CreateIcon, CancelIcon } from "@/components/ui/icons";
import { toast } from "sonner";
import {
  createBrand,
  deleteBrand,
  listTrashedBrands,
  restoreBrand,
  searchBrands,
  updateBrand,
  type BrandDto,
  type CreateBrandInput,
  type UpdateBrandInput,
} from "@/api/catalog";
import { searchGlobalBrands, type GlobalBrandSuggestion } from "@/api/catalog-global";
import { GlobalSuggestionField } from "@/components/catalog/global-suggestion-field";
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
  FormActions,
  FormGrid,
} from "@/components/list";
import { rules, validateSchema } from "@/lib/validation/rules";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
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
  | { mode: "delete"; brand: BrandDto }
  | { mode: "restore"; brand: BrandDto };

type BrandRow = BrandDto & { activeLabel: string };

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
function BrandCountryCell(row: BrandRow) {
  if (!row.countryOfOrigin) return null;
  return (
    <span className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-semibold uppercase text-[var(--color-foreground)]">
      {row.countryOfOrigin}
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  KPI card
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
  const [nameFilter, setNameFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });
  const [trashOpen, setTrashOpen] = useState(false);

  const query = useQuery({
    queryKey: ["catalog", "brands", "list"],
    queryFn: () => searchBrands({ pageSize: 200, sort: "name" }),
    placeholderData: keepPreviousData,
  });

  const trashQuery = useQuery({
    queryKey: ["catalog", "brands", "trash"],
    queryFn: () => listTrashedBrands(1, 200),
    enabled: trashOpen,
    placeholderData: keepPreviousData,
  });

  const allItems = query.data?.items ?? [];

  const rows: BrandRow[] = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    const active = triToBool(activeFilter);
    return allItems
      .filter((b) =>
        (!name || b.name.toLowerCase().includes(name) || b.slug.includes(name)) &&
        (active === undefined || b.isActive === active))
      .map((b) => ({
        ...b,
        activeLabel: b.isActive ? tc("status.active") : tc("status.inactive"),
      }));
  }, [allItems, nameFilter, activeFilter, tc]);

  const trashedRows: BrandRow[] = useMemo(() =>
    (trashQuery.data?.items ?? []).map((b) => ({
      ...b,
      activeLabel: b.isActive ? tc("status.active") : tc("status.inactive"),
    })),
    [trashQuery.data, tc],
  );

  const kpiTotal = query.data?.totalCount ?? allItems.length;
  const kpiActive = allItems.filter((b) => b.isActive).length;
  const kpiTrashed = trashQuery.data?.totalCount ?? 0;

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "logoUrl", headerText: t("brands.fields.image"), template: BrandLogoCell as any, width: 72, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("brands.singular"), template: BrandNameCell as any, minWidth: 220 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isActive", headerText: t("brands.fields.active"), template: BrandActiveCell as any, width: 110, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "countryOfOrigin", headerText: t("brands.fields.country"), template: BrandCountryCell as any, width: 100, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "slug", headerText: t("brands.fields.slug"), template: BrandSlugCell as any, width: 170 },
    ],
    [t],
  );

  const trashColumns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("brands.singular"), template: BrandNameCell as any, minWidth: 180 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "slug", headerText: t("brands.fields.slug"), template: BrandSlugCell as any, width: 160 },
    ],
    [t],
  );

  const resetFilters = () => {
    setNameFilter("");
    setActiveFilter(null);
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
          onClick={() => setTrashOpen((v) => !v)}
          aria-pressed={trashOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Trash2 className="size-4" />
          {t("brands.actions.viewTrash")}
          {kpiTrashed > 0 && (
            <span className="ml-1 rounded-full bg-[var(--color-destructive)] px-1.5 py-0.5 text-[10px] font-bold text-white">
              {kpiTrashed}
            </span>
          )}
        </Button>
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

      {trashOpen && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-[var(--color-foreground)]">
              {t("brands.trash.title")}
            </h3>
            <Button variant="ghost" size="sm" onClick={() => setTrashOpen(false)}>
              {tc("actions.close")}
            </Button>
          </div>
          <MakaGridClient<BrandRow>
            dataSource={trashedRows}
            columns={trashColumns}
            isLoading={trashQuery.isFetching}
            fileName="marcas-papelera"
            entityName={t("brands.singular")}
            permissions={{ edit: P.catalog.brands.restore }}
            onEdit={(row) => setEditor({ mode: "restore", brand: row })}
          />
        </div>
      )}

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("brands.singular")} className="grow">
              <MakaFilterInput
                value={nameFilter}
                onChange={setNameFilter}
                placeholder={t("brands.filters.namePlaceholder")}
                ariaLabel={t("brands.singular")}
                className="min-w-48"
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
          </>
        }
        kpis={
          <>
            <KpiCard label={t("brands.kpi.total")} value={kpiTotal} tone="var(--color-primary)" />
            <KpiCard label={t("brands.kpi.active")} value={kpiActive} tone="var(--color-success)" />
            {trashOpen && <KpiCard label={t("brands.kpi.trashed")} value={kpiTrashed} tone="var(--color-destructive)" />}
          </>
        }
      />

      <MakaGridClient<BrandRow>
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
      />

      <BrandEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteBrandDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <RestoreBrandDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Brand avatar
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
      name: brand?.name ?? "",
      description: brand?.description ?? "",
      logoUrl: brand?.logoUrl ?? "",
      websiteUrl: brand?.websiteUrl ?? "",
      countryOfOrigin: brand?.countryOfOrigin ?? "",
      isActive: brand?.isActive ?? true,
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [brand?.id],
  );

  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [logoUrl, setLogoUrl] = useState(initial.logoUrl);
  const [websiteUrl, setWebsiteUrl] = useState(initial.websiteUrl);
  const [countryOfOrigin, setCountryOfOrigin] = useState(initial.countryOfOrigin);
  const [isActive, setIsActive] = useState(initial.isActive);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setDescription(initial.description);
      setLogoUrl(initial.logoUrl);
      setWebsiteUrl(initial.websiteUrl);
      setCountryOfOrigin(initial.countryOfOrigin);
      setIsActive(initial.isActive);
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

  const [showErrors, setShowErrors] = useState(false);
  const errs = useMemo(() => {
    if (!showErrors) return {} as Record<string, string>;
    return validateSchema(
      { name, countryOfOrigin, websiteUrl, description },
      {
        name: [rules.required(), rules.max(200)],
        countryOfOrigin: [rules.pattern(/^[A-Za-z]{2}$/)],
        websiteUrl: [rules.url()],
        description: [rules.max(2000)],
      },
      tc,
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [showErrors, name, countryOfOrigin, websiteUrl, description]);

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const v = validateSchema(
      { name, countryOfOrigin, websiteUrl, description },
      {
        name: [rules.required(), rules.max(200)],
        countryOfOrigin: [rules.pattern(/^[A-Za-z]{2}$/)],
        websiteUrl: [rules.url()],
        description: [rules.max(2000)],
      },
      tc,
    );
    if (Object.keys(v).length > 0) { setShowErrors(true); return; }
    const payload = {
      name: trimmedName,
      description: description.trim() || null,
      logoUrl: logoUrl.trim() || null,
      websiteUrl: websiteUrl.trim() || null,
      countryOfOrigin: countryOfOrigin.trim().toUpperCase() || null,
      isActive,
    };
    if (state.mode === "edit" && brand) {
      updateMutation.mutate({ brandId: brand.id, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{brand ? t("brands.actions.edit") : t("brands.actions.add")}</DialogTitle>
            <DialogDescription>
              {brand ? t("brands.editDesc", { name: brand.name }) : t("brands.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="brand-name" span={8} label={t("brands.fields.name")} required error={errs.name}
                hint={!brand ? t("globalSuggest.fieldHint") : undefined}>
                {brand ? (
                  <Input id="brand-name" value={name} onChange={(e) => setName(e.target.value)}
                    placeholder={t("brands.namePlaceholder")} autoFocus required maxLength={200} />
                ) : (
                  <GlobalSuggestionField<GlobalBrandSuggestion>
                    id="brand-name" value={name} onChange={(v) => setName(v)}
                    search={searchGlobalBrands} queryKey="brands"
                    toItem={(s) => ({ key: s.id, primary: s.name, secondary: s.country, adopted: s.alreadyAdopted })}
                    onPick={(s) => {
                      setName(s.name);
                      if (s.country) setCountryOfOrigin(s.country.toUpperCase());
                      if (s.logoUrl) setLogoUrl(s.logoUrl);
                    }}
                    placeholder={t("brands.namePlaceholder")} maxLength={200} autoFocus
                  />
                )}
              </Field>

              <Field id="brand-country" span={4} label={t("brands.fields.country")} error={errs.countryOfOrigin} hint={t("brands.countryHint")}>
                <Input
                  id="brand-country"
                  value={countryOfOrigin}
                  onChange={(e) => setCountryOfOrigin(e.target.value.toUpperCase())}
                  placeholder={t("brands.countryPlaceholder")}
                  maxLength={2}
                  className="uppercase"
                />
              </Field>

              <Field id="brand-slug" span={6} label={t("brands.fields.slug")} hint={t("brands.slugHint")}>
                <div className="flex h-9 items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                  <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                    {slugPreview}
                  </code>
                </div>
              </Field>

              <Field id="brand-logo" span={6} label={t("brands.fields.image")} hint={t("brands.logoHint")}>
                <ImageInput value={logoUrl} onChange={setLogoUrl} ownerType="Brand" ownerId={brand?.id} shape="square" />
              </Field>

              <Field id="brand-website" span={12} label={t("brands.fields.website")} error={errs.websiteUrl} hint={t("brands.websiteHint")}>
                <Input
                  id="brand-website"
                  value={websiteUrl}
                  onChange={(e) => setWebsiteUrl(e.target.value)}
                  placeholder={t("brands.websiteUrlPlaceholder")}
                  maxLength={500}
                  type="url"
                />
              </Field>

              <Field id="brand-description" span={12} label={t("brands.fields.description")} error={errs.description} hint={t("brands.descriptionHint")}>
                <textarea
                  id="brand-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  maxLength={2000}
                  className={cn(
                    "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                    "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                    "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                  )}
                  placeholder={t("brands.descPlaceholder")}
                />
              </Field>

              <div className="col-span-1 flex items-center gap-8 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("brands.fields.active")} />
                  {t("brands.fields.active")}
                </label>
              </div>

              {brand && (
                <div className="col-span-1 border-t border-[var(--color-border)] pt-4 sm:col-span-12">
                  <EntityAuditSection entityKey={brand.id} entityName="Brand" />
                </div>
              )}
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <FormActions
              secondary={
                <DialogClose asChild>
                  <Button type="button" variant="outline" disabled={isPending}>
                    <CancelIcon className="size-4" />{tc("actions.cancel")}
                  </Button>
                </DialogClose>
              }
              primary={
                <Button type="submit" disabled={isPending}>
                  {brand ? <SaveIcon className="size-4" /> : <CreateIcon className="size-4" />}
                  {isPending ? tc("feedback.saving") : brand ? tc("actions.saveChanges") : t("brands.actions.add")}
                </Button>
              }
            />
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

// ───────────────────────────────────────────────────────────────────────
//  Restore confirmation
// ───────────────────────────────────────────────────────────────────────

function RestoreBrandDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "restore";
  const brand = state.mode === "restore" ? state.brand : undefined;
  const queryClient = useQueryClient();

  const restoreMutation = useMutation({
    mutationFn: (id: string) => restoreBrand(id),
    onSuccess: () => {
      toast.success(t("brands.trash.restoreSuccess"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error(t("brands.trash.restoreFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("brands.trash.restoreConfirmTitle", { name: brand?.name ?? "" })}</DialogTitle>
          <DialogDescription>{t("brands.trash.restoreConfirmDesc")}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={restoreMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            onClick={() => brand && restoreMutation.mutate(brand.id)}
            disabled={restoreMutation.isPending || !brand}
          >
            {restoreMutation.isPending ? tc("feedback.saving") : t("brands.actions.restore")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
