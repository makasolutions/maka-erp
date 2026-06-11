import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, Layers, Plus, SlidersHorizontal, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import {
  addAttributeValue,
  createAttribute,
  deleteAttribute,
  getAttributeById,
  getCategoryTree,
  removeAttributeValue,
  searchAttributes,
  updateAttribute,
  ATTRIBUTE_TYPES,
  type AttributeDto,
  type CatalogAttributeType,
  type CategoryDto,
  type CreateAttributeInput,
  type UpdateAttributeInput,
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
  FormActions,
  FormGrid,
} from "@/components/list";
import { SaveIcon, CreateIcon, CancelIcon } from "@/components/ui/icons";
import { rules, validateSchema } from "@/lib/validation/rules";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
  MakaKpiCard,
} from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe, slugify } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; attribute: AttributeDto }
  | { mode: "delete"; attribute: AttributeDto };

type AttributeRow = AttributeDto & { typeLabel: string; variationsLabel: string; categoryLabel: string };

function triToBool(v: string | null): boolean | undefined {
  return v === null ? undefined : v === "true";
}

type FlatCategory = { id: string; label: string };
function flattenCategories(nodes: CategoryDto[], depth = 0, acc: FlatCategory[] = []): FlatCategory[] {
  for (const n of nodes) {
    acc.push({ id: n.id, label: `${"— ".repeat(depth)}${n.name}` });
    if (n.children?.length) flattenCategories(n.children, depth + 1, acc);
  }
  return acc;
}

// ── Cell templates ──────────────────────────────────────────────────────

function AttrNameCell(row: AttributeRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">{row.slug}</code>
    </div>
  );
}
function AttrTypeCell(row: AttributeRow) {
  return <EntityStatusBadge tone="info">{row.typeLabel}</EntityStatusBadge>;
}
function AttrVariationsCell(row: AttributeRow) {
  return (
    <EntityStatusBadge tone={row.isUsedForVariations ? "success" : "default"}>
      {row.variationsLabel}
    </EntityStatusBadge>
  );
}
function AttrCategoriesCell(row: AttributeRow) {
  if (!row.categoryLabel) return <span className="text-[12px] text-[var(--color-muted-foreground)]">—</span>;
  return (
    <span className="truncate text-[12px] text-[var(--color-foreground)]" title={row.categoryLabel}>
      {row.categoryLabel}
    </span>
  );
}
function AttrValueCountCell(row: AttributeRow) {
  return (
    <span className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-semibold text-[var(--color-foreground)]">
      {row.valueCount}
    </span>
  );
}

// ── KPI ─────────────────────────────────────────────────────────────────

// ── Page ──────────────────────────────────────────────────────────────────

export function AttributesPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [variationsFilter, setVariationsFilter] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: ["catalog", "attributes", "list"],
    queryFn: () => searchAttributes({ pageSize: 200, sort: "name" }),
    placeholderData: keepPreviousData,
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: () => getCategoryTree(),
    placeholderData: keepPreviousData,
  });
  const categoryNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of flattenCategories(categoriesQuery.data ?? [])) map.set(c.id, c.label.replace(/—\s/g, ""));
    return map;
  }, [categoriesQuery.data]);

  const allItems = query.data?.items ?? [];

  const typeLabel = (type: CatalogAttributeType) => t(`attributes.type.${type}`);

  const rows: AttributeRow[] = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    const usedForVariations = triToBool(variationsFilter);
    return allItems
      .filter(
        (a) =>
          (!name || a.name.toLowerCase().includes(name) || a.slug.includes(name)) &&
          (usedForVariations === undefined || a.isUsedForVariations === usedForVariations),
      )
      .map((a) => ({
        ...a,
        typeLabel: typeLabel(a.type),
        variationsLabel: a.isUsedForVariations ? tc("status.yes") : tc("status.no"),
        categoryLabel: (a.categoryIds ?? []).map((id) => categoryNames.get(id)).filter(Boolean).join(", "),
      }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allItems, nameFilter, variationsFilter, tc, t, categoryNames]);

  const kpiTotal = query.data?.totalCount ?? allItems.length;
  const kpiForVariations = allItems.filter((a) => a.isUsedForVariations).length;

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("attributes.fields.name"), template: AttrNameCell as any, minWidth: 220 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "type", headerText: t("attributes.fields.type"), template: AttrTypeCell as any, width: 120, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "categoryLabel", headerText: t("attributes.fields.categories"), template: AttrCategoriesCell as any, minWidth: 180, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isUsedForVariations", headerText: t("attributes.fields.usedForVariations"), template: AttrVariationsCell as any, width: 150, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "valueCount", headerText: t("attributes.fields.valueCount"), template: AttrValueCountCell as any, width: 120, allowSorting: false, textAlign: "Center" },
    ],
    [t],
  );

  const resetFilters = () => {
    setNameFilter("");
    setVariationsFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={SlidersHorizontal}
        title={t("attributes.title")}
        total={query.data?.totalCount ?? null}
        unit={t("attributes.singular")}
        description={t("attributes.description")}
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
          perm={P.catalog.attributes.manage}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("attributes.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("attributes.singular")} className="grow">
              <MakaFilterInput
                value={nameFilter}
                onChange={setNameFilter}
                placeholder={t("attributes.filters.namePlaceholder")}
                ariaLabel={t("attributes.singular")}
                className="min-w-48"
              />
            </MakaFilterField>
            <MakaFilterField label={t("attributes.filters.usedForVariations")}>
              <EntityFilterPill<string | null>
                label={t("attributes.filters.usedForVariations")}
                value={variationsFilter}
                onChange={setVariationsFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: tc("status.yes") },
                  { value: "false", label: tc("status.no") },
                ]}
              />
            </MakaFilterField>
          </>
        }
        kpis={
          <>
            <MakaKpiCard icon={SlidersHorizontal} label={t("attributes.kpi.total")} value={kpiTotal} tone="var(--color-primary)" />
            <MakaKpiCard icon={Layers} label={t("attributes.kpi.forVariations")} value={kpiForVariations} tone="var(--color-success)" />
          </>
        }
      />

      <MakaGridClient<AttributeRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="atributos"
        entityName={t("attributes.singular")}
        onRowClick={(row) => can(P.catalog.attributes.manage) && setEditor({ mode: "edit", attribute: row })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.attributes.manage, delete: P.catalog.attributes.manage }}
        onEdit={(row) => setEditor({ mode: "edit", attribute: row })}
        onDelete={(row) => setEditor({ mode: "delete", attribute: row })}
      />

      <AttributeEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteAttributeDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ── Editor dialog (with value manager in edit mode) ───────────────────────

function AttributeEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const attribute = state.mode === "edit" ? state.attribute : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: attribute?.name ?? "",
      type: (attribute?.type ?? "Select") as CatalogAttributeType,
      isUsedForVariations: attribute?.isUsedForVariations ?? true,
      isVisibleOnProduct: attribute?.isVisibleOnProduct ?? true,
      sortOrder: attribute?.sortOrder ?? 0,
      categoryIds: attribute?.categoryIds ?? [],
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [attribute?.id],
  );

  const [name, setName] = useState(initial.name);
  const [type, setType] = useState<CatalogAttributeType>(initial.type);
  const [isUsedForVariations, setIsUsedForVariations] = useState(initial.isUsedForVariations);
  const [isVisibleOnProduct, setIsVisibleOnProduct] = useState(initial.isVisibleOnProduct);
  const [sortOrder, setSortOrder] = useState(initial.sortOrder);
  const [categoryIds, setCategoryIds] = useState<string[]>(initial.categoryIds);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setType(initial.type);
      setIsUsedForVariations(initial.isUsedForVariations);
      setIsVisibleOnProduct(initial.isVisibleOnProduct);
      setSortOrder(initial.sortOrder);
      setCategoryIds(initial.categoryIds);
    }
  }, [isOpen, initial]);

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: () => getCategoryTree(),
    enabled: isOpen,
    placeholderData: keepPreviousData,
  });
  const flatCategories = useMemo(() => flattenCategories(categoriesQuery.data ?? []), [categoriesQuery.data]);
  const toggleCategory = (id: string, on: boolean) =>
    setCategoryIds((prev) => (on ? [...new Set([...prev, id])] : prev.filter((x) => x !== id)));

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);

  const createMutation = useMutation({
    mutationFn: (input: CreateAttributeInput) => createAttribute(input),
    onSuccess: () => {
      toast.success(tc("feedback.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "attributes"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateAttributeInput) => updateAttribute(input),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "attributes"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.updateFailed"), { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();

  const [showErrors, setShowErrors] = useState(false);
  const attrSchema = { name: [rules.required(), rules.max(128)], sortOrder: [rules.integer(), rules.number({ min: 0 })] };
  const errs = showErrors ? validateSchema({ name, sortOrder }, attrSchema, tc) : {} as Record<string, string>;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (Object.keys(validateSchema({ name, sortOrder }, attrSchema, tc)).length > 0) { setShowErrors(true); return; }
    if (state.mode === "edit" && attribute) {
      updateMutation.mutate({
        attributeId: attribute.id,
        name: trimmedName,
        type,
        isUsedForVariations,
        isVisibleOnProduct,
        sortOrder,
        categoryIds,
      });
    } else {
      createMutation.mutate({
        name: trimmedName,
        type,
        isUsedForVariations,
        isVisibleOnProduct,
        sortOrder,
        categoryIds,
      });
    }
  };

  const typeOptions = ATTRIBUTE_TYPES.map((tp) => ({ value: tp, label: t(`attributes.type.${tp}`) }));

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{attribute ? t("attributes.actions.edit") : t("attributes.actions.add")}</DialogTitle>
            <DialogDescription>
              {attribute ? t("attributes.editDesc", { name: attribute.name }) : t("attributes.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="attr-name" span={8} label={t("attributes.fields.name")} required error={errs.name}>
                <Input
                  id="attr-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("attributes.namePlaceholder")}
                  autoFocus
                  required
                  maxLength={128}
                />
              </Field>

              <Field id="attr-type" span={4} label={t("attributes.fields.type")}>
                <Combobox
                  id="attr-type"
                  label={t("attributes.fields.type")}
                  value={type}
                  onChange={(v) => v && setType(v as CatalogAttributeType)}
                  options={typeOptions}
                />
              </Field>

              <Field id="attr-slug" span={6} label={t("attributes.fields.slug")} hint={t("attributes.slugHint")}>
                <div className="flex h-9 items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                  <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                    {slugPreview}
                  </code>
                </div>
              </Field>

              <Field id="attr-sort" span={6} label={t("attributes.fields.sortOrder")} error={errs.sortOrder}>
                <Input
                  id="attr-sort"
                  type="number"
                  min={0}
                  value={sortOrder}
                  onChange={(e) => setSortOrder(Number(e.target.value) || 0)}
                />
              </Field>

              <Field id="attr-categories" span={12} label={t("attributes.fields.categories")} hint={t("attributes.categoriesHint")}>
                {flatCategories.length === 0 ? (
                  <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("attributes.noCategories")}</p>
                ) : (
                  <div className="max-h-40 overflow-y-auto rounded-md border border-[var(--color-border)] bg-[var(--color-background)] p-2">
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1.5 sm:grid-cols-3">
                      {flatCategories.map((c) => (
                        <label key={c.id} className="flex items-center gap-2 text-[12.5px] text-[var(--color-foreground)]">
                          <input
                            type="checkbox"
                            checked={categoryIds.includes(c.id)}
                            onChange={(e) => toggleCategory(c.id, e.target.checked)}
                            className="size-3.5 accent-[var(--color-primary)]"
                          />
                          <span className="truncate" title={c.label}>{c.label}</span>
                        </label>
                      ))}
                    </div>
                  </div>
                )}
              </Field>

              <div className="col-span-1 flex flex-col gap-3 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch
                    checked={isUsedForVariations}
                    onCheckedChange={setIsUsedForVariations}
                    aria-label={t("attributes.fields.usedForVariations")}
                  />
                  {t("attributes.fields.usedForVariations")}
                  <span className="text-[12px] font-normal text-[var(--color-muted-foreground)]">
                    — {t("attributes.usedForVariationsHint")}
                  </span>
                </label>
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch
                    checked={isVisibleOnProduct}
                    onCheckedChange={setIsVisibleOnProduct}
                    aria-label={t("attributes.fields.visibleOnProduct")}
                  />
                  {t("attributes.fields.visibleOnProduct")}
                  <span className="text-[12px] font-normal text-[var(--color-muted-foreground)]">
                    — {t("attributes.visibleHint")}
                  </span>
                </label>
              </div>

              {attribute && (
                <div className="col-span-1 border-t border-[var(--color-border)] pt-4 sm:col-span-12">
                  <AttributeValuesManager attributeId={attribute.id} type={type} />
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
                  {attribute ? <SaveIcon className="size-4" /> : <CreateIcon className="size-4" />}
                  {isPending ? tc("feedback.saving") : attribute ? tc("actions.saveChanges") : t("attributes.actions.add")}
                </Button>
              }
            />
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Values manager (nested under attribute editor) ────────────────────────

function AttributeValuesManager({ attributeId, type }: { attributeId: string; type: CatalogAttributeType }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isColor = type === "Color";

  const [newValue, setNewValue] = useState("");
  const [newColor, setNewColor] = useState("#000000");

  const detail = useQuery({
    queryKey: ["catalog", "attributes", "detail", attributeId],
    queryFn: () => getAttributeById(attributeId),
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["catalog", "attributes", "detail", attributeId] });
    queryClient.invalidateQueries({ queryKey: ["catalog", "attributes", "list"] });
  };

  const addMutation = useMutation({
    mutationFn: () =>
      addAttributeValue(attributeId, {
        value: newValue.trim(),
        colorCode: isColor ? newColor : null,
      }),
    onSuccess: () => {
      setNewValue("");
      invalidate();
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const removeMutation = useMutation({
    mutationFn: (valueId: string) => removeAttributeValue(attributeId, valueId),
    onSuccess: invalidate,
    onError: (err) => toast.error(tc("feedback.deleteFailed"), { description: describe(err) }),
  });

  const values = detail.data?.values ?? [];

  return (
    <div className="space-y-3">
      <div className="text-[13px] font-semibold text-[var(--color-foreground)]">{t("attributes.values.title")}</div>

      {values.length === 0 ? (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("attributes.values.empty")}</p>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {values.map((v) => (
            <li
              key={v.id}
              className="flex items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2.5 py-1.5 text-[12.5px]"
            >
              {v.colorCode && (
                <span
                  aria-hidden
                  className="size-3.5 rounded-full ring-1 ring-inset ring-[var(--color-border)]"
                  style={{ backgroundColor: v.colorCode }}
                />
              )}
              <span className="font-medium text-[var(--color-foreground)]">{v.value}</span>
              <button
                type="button"
                onClick={() => removeMutation.mutate(v.id)}
                disabled={removeMutation.isPending}
                aria-label={t("attributes.values.removeConfirm")}
                className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-destructive)]"
              >
                <Trash2 className="size-3.5" />
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-wrap items-end gap-2">
        <div className="grow">
          <Input
            value={newValue}
            onChange={(e) => setNewValue(e.target.value)}
            placeholder={t("attributes.values.valuePlaceholder")}
            maxLength={128}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                if (newValue.trim()) addMutation.mutate();
              }
            }}
          />
        </div>
        {isColor && (
          <input
            type="color"
            value={newColor}
            onChange={(e) => setNewColor(e.target.value)}
            aria-label={t("attributes.fields.color")}
            className="h-9 w-12 cursor-pointer rounded-md border border-[var(--color-border)] bg-transparent p-1"
          />
        )}
        <Button
          type="button"
          variant="outline"
          disabled={!newValue.trim() || addMutation.isPending}
          onClick={() => addMutation.mutate()}
        >
          <Plus className="size-4" />
          {t("attributes.values.addValue")}
        </Button>
      </div>
    </div>
  );
}

// ── Delete confirmation ───────────────────────────────────────────────────

function DeleteAttributeDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "delete";
  const attribute = state.mode === "delete" ? state.attribute : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteAttribute(id),
    onSuccess: () => {
      toast.success(tc("feedback.deleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "attributes"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("attributes.actions.delete")}</DialogTitle>
          <DialogDescription>{t("attributes.deleteDesc", { name: attribute?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              <X className="size-4" />{tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => attribute && deleteMutation.mutate(attribute.id)}
            disabled={deleteMutation.isPending || !attribute}
          >
            <Trash2 className="size-4" />{deleteMutation.isPending ? tc("feedback.deleting") : t("attributes.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
