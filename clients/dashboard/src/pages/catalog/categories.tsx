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
import { ChevronsRight, Eye, GitBranch, Layers, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  createCategory,
  deleteCategory,
  getCategoryTree,
  listTrashedCategories,
  restoreCategory,
  searchCategories,
  updateCategory,
  type CategoryDto,
  type CreateCategoryInput,
  type UpdateCategoryInput,
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
  Combobox,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
  Field,
  FormGrid,
} from "@/components/list";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import { describe, formatDate, slugify } from "@/lib/list-helpers";
import { EntityAuditSection } from "@/components/entity-audit-section";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type FlatNode = { id: string; name: string; slug: string; depth: number; ancestorIds: string[] };
function flattenTree(nodes: CategoryDto[], depth = 0, ancestors: string[] = []): FlatNode[] {
  const out: FlatNode[] = [];
  for (const n of nodes) {
    out.push({ id: n.id, name: n.name, slug: n.slug, depth, ancestorIds: ancestors });
    if (n.children?.length) out.push(...flattenTree(n.children, depth + 1, [...ancestors, n.id]));
  }
  return out;
}

type CategoryRow = CategoryDto & { parentName: string; activeLabel: string };

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; category: CategoryDto }
  | { mode: "delete"; category: CategoryDto }
  | { mode: "restore"; category: CategoryDto };

function triToBool(v: string | null): boolean | undefined {
  return v === null ? undefined : v === "true";
}

// ── Cell templates ───────────────────────────────────────────────────────
function CatImageCell(row: CategoryRow) {
  return <CategoryAvatar category={row} size={32} />;
}
function CatNameCell(row: CategoryRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      <div className="mt-0.5 flex items-center gap-1 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {row.parentId ? (
          <>
            <ChevronsRight className="size-3 shrink-0 opacity-60" />
            <span className="truncate">{row.parentName}</span>
          </>
        ) : (
          <>
            <GitBranch className="size-3 shrink-0 opacity-60" />
            <span>{row.parentName}</span>
          </>
        )}
      </div>
    </div>
  );
}
function CatSlugCell(row: CategoryRow) {
  return <code className="font-mono text-[11.5px] text-[var(--color-muted-foreground)]">{row.slug}</code>;
}
function CatActiveCell(row: CategoryRow) {
  return <EntityStatusBadge tone={row.isActive ? "success" : "default"}>{row.activeLabel}</EntityStatusBadge>;
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

export function CategoriesPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });
  const [trashOpen, setTrashOpen] = useState(false);

  const query = useQuery({
    queryKey: ["catalog", "categories", "list"],
    queryFn: () => searchCategories({ pageSize: 200 }),
    placeholderData: keepPreviousData,
  });

  const treeQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: getCategoryTree,
    staleTime: 30_000,
  });

  const trashQuery = useQuery({
    queryKey: ["catalog", "categories", "trash"],
    queryFn: () => listTrashedCategories(1, 200),
    enabled: trashOpen,
    placeholderData: keepPreviousData,
  });

  const allItems = query.data?.items ?? [];

  const nameById = useMemo(() => {
    const map = new Map<string, string>();
    const walk = (nodes: CategoryDto[]) => {
      for (const n of nodes) {
        map.set(n.id, n.name);
        if (n.children?.length) walk(n.children);
      }
    };
    if (treeQuery.data) walk(treeQuery.data);
    return map;
  }, [treeQuery.data]);

  const rows: CategoryRow[] = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    const active = triToBool(activeFilter);
    return allItems
      .filter((c) =>
        (!name || c.name.toLowerCase().includes(name) || c.slug.includes(name)) &&
        (active === undefined || c.isActive === active))
      .map((c) => ({
        ...c,
        parentName: c.parentId
          ? nameById.get(c.parentId) ?? "—"
          : t("categories.rootLabel"),
        activeLabel: c.isActive ? tc("status.active") : tc("status.inactive"),
      }));
  }, [allItems, nameById, nameFilter, activeFilter, t, tc]);

  const trashedRows: CategoryRow[] = useMemo(() =>
    (trashQuery.data?.items ?? []).map((c) => ({
      ...c,
      children: c.children ?? [],
      parentName: c.parentId ? nameById.get(c.parentId) ?? "—" : t("categories.rootLabel"),
      activeLabel: c.isActive ? tc("status.active") : tc("status.inactive"),
    })),
    [trashQuery.data, nameById, t, tc],
  );

  const kpiTotal = query.data?.totalCount ?? allItems.length;
  const kpiActive = allItems.filter((c) => c.isActive).length;
  const kpiTrashed = trashQuery.data?.totalCount ?? 0;

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "imageUrl", headerText: t("categories.fields.image"), template: CatImageCell as any, width: 72, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("categories.singular"), template: CatNameCell as any, minWidth: 240 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isActive", headerText: t("categories.fields.active"), template: CatActiveCell as any, width: 110, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "slug", headerText: t("categories.fields.slug"), template: CatSlugCell as any, width: 170 },
      { field: "sortOrder", headerText: t("categories.fields.sortOrder"), width: 100, textAlign: "Center" },
    ],
    [t],
  );

  const trashColumns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("categories.singular"), template: CatNameCell as any, minWidth: 200 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "slug", headerText: t("categories.fields.slug"), template: CatSlugCell as any, width: 160 },
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
        icon={Layers}
        title={t("categories.title")}
        total={query.data?.totalCount ?? null}
        unit={t("categories.singular")}
        description={t("categories.description")}
      >
        <Button
          variant="outline"
          onClick={() => setTrashOpen((v) => !v)}
          aria-pressed={trashOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Trash2 className="size-4" />
          {t("categories.actions.viewTrash")}
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
          perm={P.catalog.categories.create}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("categories.actions.create")}
        </Button>
      </EntityPageHeader>

      {trashOpen && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-[var(--color-foreground)]">
              {t("categories.trash.title")}
            </h3>
            <Button variant="ghost" size="sm" onClick={() => setTrashOpen(false)}>
              {tc("actions.close")}
            </Button>
          </div>
          <MakaGridClient<CategoryRow>
            dataSource={trashedRows}
            columns={trashColumns}
            isLoading={trashQuery.isFetching}
            fileName="categorias-papelera"
            entityName={t("categories.singular")}
            permissions={{ edit: P.catalog.categories.update }}
            onEdit={(row) => setEditor({ mode: "restore", category: row })}
          />
        </div>
      )}

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("categories.singular")} className="grow">
              <MakaFilterInput
                value={nameFilter}
                onChange={setNameFilter}
                placeholder={t("categories.filters.namePlaceholder")}
                ariaLabel={t("categories.singular")}
                className="min-w-48"
              />
            </MakaFilterField>
            <MakaFilterField label={t("categories.fields.active")}>
              <EntityFilterPill<string | null>
                label={t("categories.fields.active")}
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
            <KpiCard label={t("categories.kpi.total")} value={kpiTotal} tone="var(--color-primary)" />
            <KpiCard label={t("categories.kpi.active")} value={kpiActive} tone="var(--color-success)" />
            <KpiCard label={t("categories.kpi.trashed")} value={kpiTrashed} tone="var(--color-destructive)" />
          </>
        }
      />

      <MakaGridClient<CategoryRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="categorias"
        entityName={t("categories.singular")}
        onRowClick={(row) => can(P.catalog.categories.update) && setEditor({ mode: "edit", category: row })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.categories.update, delete: P.catalog.categories.delete }}
        onEdit={(row) => setEditor({ mode: "edit", category: row })}
        onDelete={(row) => setEditor({ mode: "delete", category: row })}
      />

      <CategoryEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        tree={treeQuery.data ?? []}
      />
      <DeleteCategoryDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <RestoreCategoryDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ── Category avatar ────────────────────────────────────────────────────────
function CategoryAvatar({ category, size }: { category: CategoryDto; size: number }) {
  if (category.imageUrl) {
    return (
      <span
        style={{ width: size, height: size }}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-lg",
          "bg-[var(--color-muted)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img src={category.imageUrl} alt="" className="h-full w-full object-cover" loading="lazy" referrerPolicy="no-referrer" />
      </span>
    );
  }
  return <EntityInitialsAvatar name={category.name} size={size} />;
}

// ───────────────────────────────────────────────────────────────────────
//  Editor dialog
// ───────────────────────────────────────────────────────────────────────

function CategoryEditorDialog({
  state,
  onClose,
  tree,
}: {
  state: EditorState;
  onClose: () => void;
  tree: CategoryDto[];
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const category = state.mode === "edit" ? state.category : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: category?.name ?? "",
      description: category?.description ?? "",
      imageUrl: category?.imageUrl ?? "",
      parentId: category?.parentId ?? "",
      sortOrder: category?.sortOrder ?? 0,
      isActive: category?.isActive ?? true,
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [category?.id],
  );

  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [imageUrl, setImageUrl] = useState(initial.imageUrl);
  const [parentId, setParentId] = useState(initial.parentId);
  const [sortOrder, setSortOrder] = useState(initial.sortOrder);
  const [isActive, setIsActive] = useState(initial.isActive);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setDescription(initial.description);
      setImageUrl(initial.imageUrl);
      setParentId(initial.parentId);
      setSortOrder(initial.sortOrder);
      setIsActive(initial.isActive);
    }
  }, [isOpen, initial]);

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);

  const parentOptions = useMemo<FlatNode[]>(() => {
    const flat = flattenTree(tree);
    if (!category) return flat;
    return flat.filter((n) => n.id !== category.id && !n.ancestorIds.includes(category.id));
  }, [tree, category]);

  const createMutation = useMutation({
    mutationFn: (input: CreateCategoryInput) => createCategory(input),
    onSuccess: () => {
      toast.success(t("categories.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      onClose();
    },
    onError: (err) => toast.error(t("categories.createFailed"), { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateCategoryInput) => updateCategory(input),
    onSuccess: () => {
      toast.success(t("categories.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      onClose();
    },
    onError: (err) => toast.error(t("categories.updateFailed"), { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();
  const canSubmit = !!trimmedName;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!canSubmit) return;
    const payload = {
      name: trimmedName,
      description: description.trim() || null,
      imageUrl: imageUrl.trim() || null,
      parentId: parentId || null,
      sortOrder,
      isActive,
    };
    if (state.mode === "edit" && category) {
      updateMutation.mutate({ categoryId: category.id, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{category ? t("categories.editTitle") : t("categories.createTitle")}</DialogTitle>
            <DialogDescription>
              {category ? t("categories.editDesc", { name: category.name }) : t("categories.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="category-name" span={8} label={t("categories.fields.name")} required>
                <Input
                  id="category-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("categories.namePlaceholder")}
                  autoFocus
                  required
                  maxLength={200}
                />
              </Field>

              <Field id="category-sort" span={4} label={t("categories.fields.sortOrder")} hint={t("categories.sortOrderHint")}>
                <Input
                  id="category-sort"
                  type="number"
                  min={0}
                  value={sortOrder}
                  onChange={(e) => setSortOrder(Number(e.target.value))}
                />
              </Field>

              <Field id="category-slug" span={6} label={t("categories.fields.slug")} hint={t("categories.slugHint")}>
                <div className="flex h-9 items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                  <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                    {slugPreview}
                  </code>
                </div>
              </Field>

              <Field id="category-parent" span={6} label={t("categories.fields.parent")} hint={t("categories.parentHint")}>
                <Combobox
                  id="category-parent"
                  label={t("categories.fields.parent")}
                  placeholder={t("categories.noParentPlaceholder")}
                  value={parentId || null}
                  onChange={(v) => setParentId(v ?? "")}
                  options={parentOptions.map((opt) => ({
                    value: opt.id,
                    label: opt.name,
                    prefix:
                      opt.depth > 0 ? (
                        <span
                          aria-hidden
                          className="font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]"
                          style={{ paddingLeft: `${(opt.depth - 1) * 12}px` }}
                        >
                          ↳
                        </span>
                      ) : (
                        <span
                          aria-hidden
                          className="text-[10.5px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]"
                        >
                          {t("categories.rootLabel")}
                        </span>
                      ),
                  }))}
                  searchable
                  clearable
                  emptyOptionLabel={t("categories.noParentPlaceholder")}
                />
              </Field>

              <Field id="category-description" span={12} label={t("categories.fields.description")} hint={t("categories.descriptionHint")}>
                <textarea
                  id="category-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  maxLength={2000}
                  className={cn(
                    "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                    "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                    "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                  )}
                  placeholder={t("categories.descPlaceholder")}
                />
              </Field>

              <Field id="category-image" span={12} label={t("categories.fields.image")} hint={t("categories.imageHint")}>
                <ImageInput value={imageUrl} onChange={setImageUrl} ownerType="Category" ownerId={category?.id} shape="square" />
              </Field>

              <div className="col-span-1 flex items-center gap-8 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("categories.fields.active")} />
                  {t("categories.fields.active")}
                </label>
              </div>

              {category && (
                <div className="col-span-1 border-t border-[var(--color-border)] pt-4 sm:col-span-12">
                  <EntityAuditSection entityKey={category.id} entityName="Category" />
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
              {isPending ? tc("feedback.saving") : category ? tc("actions.saveChanges") : t("categories.actions.add")}
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

function DeleteCategoryDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "delete";
  const category = state.mode === "delete" ? state.category : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    onSuccess: () => {
      toast.success(t("categories.deleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      onClose();
    },
    onError: (err) => toast.error(t("categories.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("categories.actions.delete")}</DialogTitle>
          <DialogDescription>
            {t("categories.deleteFullDesc", {
              name: category?.name ?? "",
              date: category ? formatDate(category.createdAtUtc ?? "") : "",
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
            onClick={() => category && deleteMutation.mutate(category.id)}
            disabled={deleteMutation.isPending || !category}
          >
            {deleteMutation.isPending ? tc("feedback.deleting") : t("categories.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Restore confirmation
// ───────────────────────────────────────────────────────────────────────

function RestoreCategoryDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "restore";
  const category = state.mode === "restore" ? state.category : undefined;
  const queryClient = useQueryClient();

  const restoreMutation = useMutation({
    mutationFn: (id: string) => restoreCategory(id),
    onSuccess: () => {
      toast.success(t("categories.restored"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      onClose();
    },
    onError: (err) => toast.error(t("categories.restoreFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("categories.trash.restoreTitle")}</DialogTitle>
          <DialogDescription>
            {t("categories.trash.restoreDesc", { name: category?.name ?? "" })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={restoreMutation.isPending}>
              {tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            onClick={() => category && restoreMutation.mutate(category.id)}
            disabled={restoreMutation.isPending || !category}
          >
            {restoreMutation.isPending ? tc("feedback.saving") : t("categories.trash.restoreAction")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
