import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, Hash, ImageIcon, Layers, Plus, SlidersHorizontal, Sparkles, Star, Tag, Trash2, Wand2, X } from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  addProductCode,
  addVariation,
  deleteVariation,
  generateVariations,
  getAttributeById,
  getProductById,
  getProductCodes,
  getProductImages,
  getProductMarketplaceValidation,
  getProductTags,
  getVariations,
  removeProductCode,
  searchAttributes,
  setProductAttributes,
  setProductTags,
  updateVariation,
  PRODUCT_CODE_TYPES,
  type AddVariationInput,
  type AttributeDto,
  type ProductAttributeAssignmentInput,
  type ProductCodeType,
  type UpdateVariationInput,
  type VariationDto,
} from "@/api/catalog";
import { ProductImageManager } from "@/components/file/product-image-manager";
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
  EntityStatusBadge,
  Field,
  FormGrid,
} from "@/components/list";
import { MakaGridClient } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

export const WEIGHT_UNITS = ["KG", "G", "LB", "OZ"];

export type VarEditor =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; variation: VariationDto }
  | { mode: "delete"; variation: VariationDto }
  | { mode: "codes"; variation: VariationDto };

// ── Cell templates ───────────────────────────────────────────────────────────
export function VarSkuCell(row: VariationDto) {
  return (
    <div className="flex items-center gap-2">
      <code className="font-mono text-[13px] font-medium text-[var(--color-foreground)]">{row.sku}</code>
      {row.isDefault && (
        <span className="rounded bg-[var(--color-primary)] px-1.5 py-0.5 text-[10px] font-semibold text-[var(--color-primary-foreground)]">
          ★
        </span>
      )}
    </div>
  );
}
export function VarDescCell(row: VariationDto) {
  return <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.description || "—"}</span>;
}
export function VarCombinationCell(row: VariationDto) {
  if (!row.attributeValues || row.attributeValues.length === 0) {
    return <span className="text-[12px] text-[var(--color-muted-foreground)]">—</span>;
  }
  return (
    <div className="flex flex-wrap gap-1">
      {row.attributeValues.map((av) => (
        <span
          key={av.valueId}
          className="inline-flex items-center gap-1 rounded bg-[var(--color-muted)] px-1.5 py-0.5 text-[11px] text-[var(--color-foreground)]"
          title={`${av.attributeName}: ${av.value}`}
        >
          {av.colorCode && (
            <span
              aria-hidden
              className="size-2.5 rounded-full ring-1 ring-inset ring-[var(--color-border)]"
              style={{ backgroundColor: av.colorCode }}
            />
          )}
          {av.value}
        </span>
      ))}
    </div>
  );
}

type DetailTab = "variations" | "attributes" | "images" | "tags";

// ───────────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────────

export function ProductDetailPage() {
  const { productId = "" } = useParams();
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const [editor, setEditor] = useState<VarEditor>({ mode: "closed" });

  const productQuery = useQuery({
    queryKey: ["catalog", "products", "detail", productId],
    queryFn: () => getProductById(productId),
    enabled: !!productId,
  });

  const variationsQuery = useQuery({
    queryKey: ["catalog", "variations", productId],
    queryFn: () => getVariations(productId),
    enabled: !!productId,
  });

  const product = productQuery.data;
  const variations = useMemo(
    () => (variationsQuery.data ?? []).filter(v => !v.isDeleted),
    [variationsQuery.data],
  );

  const StatusActiveCell = useMemo(() => (row: VariationDto) =>
    <EntityStatusBadge tone={row.isActive ? "success" : "default"}>
      {row.isActive ? tc("status.active") : tc("status.inactive")}
    </EntityStatusBadge>,
    [tc],
  );

  const columns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "sku", headerText: t("variations.fields.sku"), template: VarSkuCell as any, minWidth: 180 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "attributeValues", headerText: t("variations.combination"), template: VarCombinationCell as any, minWidth: 200, allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "description", headerText: t("variations.fields.description"), template: VarDescCell as any, minWidth: 180 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "isActive", headerText: t("variations.fields.active"), template: StatusActiveCell as any, width: 120, allowSorting: false, textAlign: "Center" },
  ], [t, StatusActiveCell]);

  const statusTone = product?.status === "Active" ? "success" : product?.status === "Draft" ? "default" : "warning";

  const [tab, setTab] = useState<DetailTab>("variations");
  const canEdit = can(P.catalog.products.update);

  const tabs: { id: DetailTab; label: string; icon: typeof Layers }[] = [
    { id: "variations", label: t("detail.tabs.variations"), icon: Layers },
    { id: "attributes", label: t("detail.tabs.attributes"), icon: SlidersHorizontal },
    { id: "images", label: t("detail.tabs.images"), icon: ImageIcon },
    { id: "tags", label: t("detail.tabs.tags"), icon: Tag },
  ];

  return (
    <div className="space-y-4 sm:space-y-6">
      <div>
        <Link
          to="/catalog/products"
          className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
        >
          <ArrowLeft className="size-4" />
          {t("variations.backToProducts")}
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <span className="grid size-10 place-items-center rounded-lg bg-[var(--color-primary)] text-[var(--color-primary-foreground)]">
            <Layers className="size-5" />
          </span>
          <div>
            <h1 className="font-display text-[22px] font-semibold leading-tight text-[var(--color-foreground)]">
              {product?.name ?? "…"}
            </h1>
            <div className="mt-0.5 flex items-center gap-2 text-[13px] text-[var(--color-muted-foreground)]">
              {product && (
                <EntityStatusBadge tone={statusTone}>
                  {t(`products.statuses.${product.status}`, product.status)}
                </EntityStatusBadge>
              )}
              {product?.brandName && <span>· {product.brandName}</span>}
              {product?.type && <span>· {t(`products.types.${product.type}`, product.type)}</span>}
            </div>
          </div>
        </div>
      </div>

      {/* Tab bar */}
      <div role="tablist" className="flex flex-wrap gap-1 border-b border-[var(--color-border)]">
        {tabs.map((tb) => {
          const active = tab === tb.id;
          const Icon = tb.icon;
          return (
            <button
              key={tb.id}
              role="tab"
              aria-selected={active}
              onClick={() => setTab(tb.id)}
              className={cn(
                "-mb-px flex items-center gap-1.5 border-b-2 px-3.5 py-2 text-[13px] font-semibold transition-colors",
                active
                  ? "border-[var(--color-primary)] text-[var(--color-foreground)]"
                  : "border-transparent text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )}
            >
              <Icon className="size-4" />
              {tb.label}
            </button>
          );
        })}
      </div>

      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        {tab === "variations" && (
          <>
            <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <Layers className="size-4 text-[var(--color-muted-foreground)]" />
                <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("variations.title")}</h2>
                <span className="rounded-full bg-[var(--color-muted)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-muted-foreground)]">
                  {variations.length}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <GenerateVariationsButton productId={productId} disabled={!canEdit} />
                <Button
                  perm={P.catalog.products.update}
                  onClick={() => setEditor({ mode: "create" })}
                  className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
                >
                  <Plus className="size-4" />
                  {t("variations.actions.add")}
                </Button>
              </div>
            </div>

            <MakaGridClient<VariationDto>
              dataSource={variations}
              columns={columns}
              isLoading={variationsQuery.isFetching}
              fileName="variaciones"
              entityName={t("variations.singular")}
              permissions={{ edit: P.catalog.products.update, delete: P.catalog.products.delete }}
              onEdit={row => setEditor({ mode: "edit", variation: row })}
              onDelete={row => setEditor({ mode: "delete", variation: row })}
              extraActions={[
                {
                  key: "codes",
                  label: t("codes.manage"),
                  icon: Hash,
                  perm: P.catalog.products.update,
                  dividerBefore: true,
                  onClick: row => setEditor({ mode: "codes", variation: row }),
                },
              ]}
            />
          </>
        )}

        {tab === "attributes" && <ProductAttributesTab productId={productId} canEdit={canEdit} />}
        {tab === "images" && <ProductImagesTab productId={productId} canEdit={canEdit} />}
        {tab === "tags" && <ProductTagsTab productId={productId} canEdit={canEdit} />}
      </div>

      <VariationEditorDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteVariationDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <CodesDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} canEdit={can(P.catalog.products.update)} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Generate variations button
// ───────────────────────────────────────────────────────────────────────────

export function GenerateVariationsButton({ productId, disabled }: { productId: string; disabled: boolean }) {
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () => generateVariations(productId),
    onSuccess: (res) => {
      toast.success(t("variations.generated", { created: res.created, skipped: res.skipped }));
      queryClient.invalidateQueries({ queryKey: ["catalog", "variations", productId] });
    },
    onError: (err) => toast.error(t("variations.generateFailed"), { description: describe(err) }),
  });

  if (disabled) return null;
  return (
    <Button
      variant="outline"
      onClick={() => mutation.mutate()}
      disabled={mutation.isPending}
      className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
    >
      <Wand2 className="size-4" />
      {mutation.isPending ? t("variations.generating") : t("variations.generate")}
    </Button>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Attributes tab — assign attributes + values, then generate
// ───────────────────────────────────────────────────────────────────────────

export function ProductAttributesTab({ productId, canEdit, categoryId }: { productId: string; canEdit: boolean; categoryId?: string | null }) {
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();

  // When the product has a category, only its attributes show (template);
  // "Ver todos" lets the user pick any attribute as a free override.
  const [showAll, setShowAll] = useState(false);
  const effectiveCategoryId = !showAll && categoryId ? categoryId : undefined;

  const attrsQuery = useQuery({
    queryKey: ["catalog", "attributes", "list", effectiveCategoryId ?? "all"],
    queryFn: () => searchAttributes({ pageSize: 200, sort: "name", categoryId: effectiveCategoryId }),
  });

  // Selection state: attributeId -> { valueIds, forVariations }
  const [selection, setSelection] = useState<Record<string, { valueIds: Set<string>; forVariations: boolean }>>({});

  const attributes = attrsQuery.data?.items ?? [];

  const toggleAttribute = (attr: AttributeDto, on: boolean) => {
    setSelection((prev) => {
      const next = { ...prev };
      if (on) next[attr.id] = { valueIds: new Set(), forVariations: attr.isUsedForVariations };
      else delete next[attr.id];
      return next;
    });
  };

  const applyMutation = useMutation({
    mutationFn: () => {
      const payload: ProductAttributeAssignmentInput[] = Object.entries(selection).map(
        ([attributeId, sel]) => ({
          attributeId,
          valueIds: [...sel.valueIds],
          isUsedForVariations: sel.forVariations,
        }),
      );
      return setProductAttributes(productId, payload);
    },
    onSuccess: () => {
      toast.success(t("detail.attributes.applied"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "variations", productId] });
    },
    onError: (err) => toast.error(t("detail.attributes.applyFailed"), { description: describe(err) }),
  });

  const canApply =
    Object.keys(selection).length > 0 &&
    Object.values(selection).every((s) => !s.forVariations || s.valueIds.size > 0);

  const filteredByCategory = !!effectiveCategoryId;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <SlidersHorizontal className="size-4 text-[var(--color-muted-foreground)]" />
          <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("detail.attributes.title")}</h2>
        </div>
        {categoryId && (
          <label className="flex items-center gap-2 text-[12.5px] text-[var(--color-foreground)]">
            <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)}
              className="size-3.5 accent-[var(--color-primary)]" />
            {t("detail.attributes.showAll")}
          </label>
        )}
      </div>
      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">
        {filteredByCategory ? t("detail.attributes.introCategory") : t("detail.attributes.intro")}
      </p>

      <MarketplaceValidationPanel productId={productId} />

      {attrsQuery.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">…</p>
      ) : attributes.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          {filteredByCategory ? t("detail.attributes.noneForCategory") : t("detail.attributes.noAttributes")}
        </p>
      ) : (
      <div className="space-y-3">
        {attributes.map((attr) => (
          <AttributeAssignmentRow
            key={attr.id}
            attribute={attr}
            selected={selection[attr.id]}
            disabled={!canEdit}
            onToggle={(on) => toggleAttribute(attr, on)}
            onToggleValue={(valueId, on) =>
              setSelection((prev) => {
                const cur = prev[attr.id];
                if (!cur) return prev;
                const valueIds = new Set(cur.valueIds);
                if (on) valueIds.add(valueId);
                else valueIds.delete(valueId);
                return { ...prev, [attr.id]: { ...cur, valueIds } };
              })
            }
            onToggleForVariations={(on) =>
              setSelection((prev) => {
                const cur = prev[attr.id];
                if (!cur) return prev;
                return { ...prev, [attr.id]: { ...cur, forVariations: on } };
              })
            }
          />
        ))}
      </div>
      )}

      {canEdit && attributes.length > 0 && (
        <div className="flex justify-end border-t border-[var(--color-border)] pt-4">
          <Button onClick={() => applyMutation.mutate()} disabled={!canApply || applyMutation.isPending}>
            <Sparkles className="size-4" />
            {applyMutation.isPending ? "…" : t("detail.attributes.apply")}
          </Button>
        </div>
      )}
    </div>
  );
}

function MarketplaceValidationPanel({ productId }: { productId: string }) {
  const { t } = useTranslation("catalog");
  const q = useQuery({
    queryKey: ["catalog", "marketplace-validation", productId],
    queryFn: () => getProductMarketplaceValidation(productId),
  });
  // Only marketplaces that actually have requirements for this product's category.
  const groups = (q.data?.marketplaces ?? []).filter((g) => g.required.length > 0);
  if (groups.length === 0) return null;

  return (
    <div className="space-y-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
      <div className="text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {t("detail.attributes.marketplaceTitle")}
      </div>
      <div className="flex flex-wrap gap-2">
        {groups.map((g) => {
          const ok = g.missing.length === 0;
          return (
            <div key={g.marketplace}
              className="flex items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-card)] px-2.5 py-1.5 text-[12.5px]">
              <span className={cn("size-2 rounded-full", ok ? "bg-[var(--color-success)]" : "bg-[var(--color-warning)]")} />
              <span className="font-medium text-[var(--color-foreground)]">{t(`marketplace.${g.marketplace}`)}</span>
              {ok ? (
                <span className="text-[var(--color-success)]">{t("detail.attributes.marketplaceOk")}</span>
              ) : (
                <span className="text-[var(--color-warning)]">
                  {t("detail.attributes.marketplaceMissing", { count: g.missing.length })}: {g.missing.map((a) => a.name).join(", ")}
                </span>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function AttributeAssignmentRow({
  attribute,
  selected,
  disabled,
  onToggle,
  onToggleValue,
  onToggleForVariations,
}: {
  attribute: AttributeDto;
  selected: { valueIds: Set<string>; forVariations: boolean } | undefined;
  disabled: boolean;
  onToggle: (on: boolean) => void;
  onToggleValue: (valueId: string, on: boolean) => void;
  onToggleForVariations: (on: boolean) => void;
}) {
  const { t } = useTranslation("catalog");
  const isOn = !!selected;

  const detail = useQuery({
    queryKey: ["catalog", "attributes", "detail", attribute.id],
    queryFn: () => getAttributeById(attribute.id),
    enabled: isOn,
  });

  const values = detail.data?.values ?? [];

  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
          <Switch checked={isOn} onCheckedChange={onToggle} disabled={disabled} aria-label={attribute.name} />
          {attribute.name}
          <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">{attribute.slug}</code>
        </label>
        {isOn && (
          <label className="flex items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]">
            <Switch
              checked={selected!.forVariations}
              onCheckedChange={onToggleForVariations}
              disabled={disabled}
              aria-label={t("detail.attributes.forVariations")}
            />
            {t("detail.attributes.forVariations")}
          </label>
        )}
      </div>

      {isOn && (
        <div className="mt-3">
          {values.length === 0 ? (
            <span className="text-[12px] text-[var(--color-muted-foreground)]">…</span>
          ) : (
            <div className="flex flex-wrap gap-1.5">
              {values.map((v) => {
                const on = selected!.valueIds.has(v.id);
                return (
                  <button
                    key={v.id}
                    type="button"
                    disabled={disabled}
                    onClick={() => onToggleValue(v.id, !on)}
                    className={cn(
                      "inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-[12.5px] transition-colors",
                      on
                        ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-foreground)]"
                        : "border-[var(--color-border)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
                    )}
                  >
                    {v.colorCode && (
                      <span
                        aria-hidden
                        className="size-3 rounded-full ring-1 ring-inset ring-[var(--color-border)]"
                        style={{ backgroundColor: v.colorCode }}
                      />
                    )}
                    {v.value}
                  </button>
                );
              })}
            </div>
          )}
          {selected!.forVariations && selected!.valueIds.size === 0 && (
            <p className="mt-2 text-[11.5px] text-[var(--color-warning)]">{t("detail.attributes.selectValues")}</p>
          )}
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Images tab
// ───────────────────────────────────────────────────────────────────────────

export function ProductImagesTab({ productId, canEdit }: { productId: string; canEdit: boolean }) {
  const imagesQuery = useQuery({
    queryKey: ["catalog", "product-images", productId],
    queryFn: () => getProductImages(productId),
    enabled: !!productId,
  });

  return (
    <ProductImageManager
      productId={productId}
      images={imagesQuery.data ?? []}
      invalidateKey={["catalog", "product-images", productId]}
      readOnly={!canEdit}
    />
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Tags tab
// ───────────────────────────────────────────────────────────────────────────

export function ProductTagsTab({ productId, canEdit }: { productId: string; canEdit: boolean }) {
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();

  const tagsQuery = useQuery({
    queryKey: ["catalog", "product-tags", productId],
    queryFn: () => getProductTags(productId),
    enabled: !!productId,
  });

  const [draft, setDraft] = useState<{ name: string; color: string | null }[]>([]);
  const [newName, setNewName] = useState("");
  const [newColor, setNewColor] = useState("#3b82f6");
  const [useColor, setUseColor] = useState(false);

  useEffect(() => {
    if (tagsQuery.data) setDraft(tagsQuery.data.map((tg) => ({ name: tg.name, color: tg.color })));
  }, [tagsQuery.data]);

  const saveMutation = useMutation({
    mutationFn: () => setProductTags(productId, draft),
    onSuccess: () => {
      toast.success(t("detail.tags.saved"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "product-tags", productId] });
    },
    onError: (err) => toast.error(t("detail.tags.saveFailed"), { description: describe(err) }),
  });

  const addDraft = () => {
    const name = newName.trim();
    if (!name) return;
    if (draft.some((d) => d.name.toLowerCase() === name.toLowerCase())) {
      setNewName("");
      return;
    }
    setDraft((prev) => [...prev, { name, color: useColor ? newColor : null }]);
    setNewName("");
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Tag className="size-4 text-[var(--color-muted-foreground)]" />
        <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("detail.tags.title")}</h2>
      </div>
      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("detail.tags.intro")}</p>

      {draft.length === 0 ? (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("detail.tags.empty")}</p>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {draft.map((tg) => (
            <li
              key={tg.name}
              className="flex items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2.5 py-1.5 text-[12.5px]"
            >
              {tg.color && (
                <span
                  aria-hidden
                  className="size-3 rounded-full ring-1 ring-inset ring-[var(--color-border)]"
                  style={{ backgroundColor: tg.color }}
                />
              )}
              <span className="font-medium text-[var(--color-foreground)]">{tg.name}</span>
              {canEdit && (
                <button
                  type="button"
                  onClick={() => setDraft((prev) => prev.filter((d) => d.name !== tg.name))}
                  aria-label={tg.name}
                  className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-destructive)]"
                >
                  <Trash2 className="size-3.5" />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canEdit && (
        <>
          <div className="flex flex-wrap items-end gap-2">
            <div className="grow">
              <Input
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder={t("detail.tags.placeholder")}
                maxLength={64}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    addDraft();
                  }
                }}
              />
            </div>
            <label className="flex h-9 items-center gap-1.5">
              <Switch checked={useColor} onCheckedChange={setUseColor} aria-label="color" />
              {useColor && (
                <input
                  type="color"
                  value={newColor}
                  onChange={(e) => setNewColor(e.target.value)}
                  className="h-9 w-12 cursor-pointer rounded-md border border-[var(--color-border)] bg-transparent p-1"
                />
              )}
            </label>
            <Button type="button" variant="outline" disabled={!newName.trim()} onClick={addDraft}>
              <Plus className="size-4" />
              {t("detail.tags.add")}
            </Button>
          </div>

          <div className="flex justify-end border-t border-[var(--color-border)] pt-4">
            <Button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
              <Check className="size-4" />{saveMutation.isPending ? "…" : t("detail.tags.save")}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Variation editor (add / edit)
// ───────────────────────────────────────────────────────────────────────────

export function VariationEditorDialog({
  productId,
  state,
  onClose,
}: {
  productId: string;
  state: VarEditor;
  onClose: () => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "create" || state.mode === "edit";
  const variation = state.mode === "edit" ? state.variation : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(() => ({
    sku: variation?.sku ?? "",
    description: variation?.description ?? "",
    isActive: variation?.isActive ?? true,
    isDefault: variation?.isDefault ?? false,
    manageStock: variation?.manageStock ?? true,
    allowBackorders: variation?.allowBackorders ?? false,
    isVirtual: variation?.isVirtual ?? false,
    weight: variation?.weight?.toString() ?? "",
    weightUnit: variation?.weightUnit ?? "KG",
  }),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  [variation?.id]);

  const [sku, setSku] = useState(initial.sku);
  const [description, setDescription] = useState(initial.description);
  const [isActive, setIsActive] = useState(initial.isActive);
  const [isDefault, setIsDefault] = useState(initial.isDefault);
  const [manageStock, setManageStock] = useState(initial.manageStock);
  const [allowBackorders, setAllowBackorders] = useState(initial.allowBackorders);
  const [isVirtual, setIsVirtual] = useState(initial.isVirtual);
  const [weight, setWeight] = useState(initial.weight);
  const [weightUnit, setWeightUnit] = useState(initial.weightUnit);

  useEffect(() => {
    if (isOpen) {
      setSku(initial.sku); setDescription(initial.description); setIsActive(initial.isActive);
      setIsDefault(initial.isDefault); setManageStock(initial.manageStock);
      setAllowBackorders(initial.allowBackorders); setIsVirtual(initial.isVirtual);
      setWeight(initial.weight); setWeightUnit(initial.weightUnit);
    }
  }, [isOpen, initial]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const weightNum = weight.trim() ? Number(weight) : null;
      if (variation) {
        const input: UpdateVariationInput = {
          description: description.trim() || null,
          isActive,
          weight: weightNum,
          weightUnit,
          manageStock,
          allowBackorders,
          soldIndividually: variation.soldIndividually,
          lowStockThreshold: variation.lowStockThreshold ?? null,
          isVirtual,
        };
        await updateVariation(productId, variation.id, input);
      } else {
        const input: AddVariationInput = {
          sku: sku.trim(),
          description: description.trim() || null,
          isDefault,
          isActive,
          weight: weightNum,
          weightUnit,
          manageStock,
          allowBackorders,
          isVirtual,
        };
        await addVariation(productId, input);
      }
    },
    onSuccess: () => {
      toast.success(variation ? t("variations.updated") : t("variations.created"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "variations", productId] });
      onClose();
    },
    onError: err => toast.error(variation ? t("variations.updateFailed") : t("variations.createFailed"), { description: describe(err) }),
  });

  const canSubmit = variation ? true : !!sku.trim();

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
            <DialogTitle>{variation ? t("variations.editTitle") : t("variations.createTitle")}</DialogTitle>
            <DialogDescription>
              {variation ? t("variations.editDesc", { sku: variation.sku }) : t("variations.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="var-sku" span={6} label={t("variations.fields.sku")} required={!variation} hint={variation ? t("variations.skuImmutable") : undefined}>
                <Input
                  id="var-sku"
                  value={sku}
                  onChange={e => setSku(e.target.value.toUpperCase())}
                  placeholder="PROD-VAR-001"
                  disabled={!!variation}
                  required={!variation}
                  maxLength={64}
                  className="font-mono"
                />
              </Field>

              <Field id="var-desc" span={6} label={t("variations.fields.description")}>
                <Input
                  id="var-desc"
                  value={description}
                  onChange={e => setDescription(e.target.value)}
                  placeholder={t("variations.descPlaceholder")}
                  maxLength={256}
                />
              </Field>

              <Field id="var-weight" span={4} label={t("variations.fields.weight")}>
                <Input id="var-weight" type="number" min={0} step="0.001" value={weight} onChange={e => setWeight(e.target.value)} />
              </Field>
              <Field id="var-wunit" span={2} label={t("variations.fields.weightUnit")}>
                <Combobox
                  id="var-wunit"
                  label={t("variations.fields.weightUnit")}
                  value={weightUnit}
                  onChange={v => setWeightUnit(v ?? "KG")}
                  options={WEIGHT_UNITS.map(u => ({ value: u, label: u }))}
                />
              </Field>

              <div className="col-span-1 flex flex-wrap items-center gap-x-8 gap-y-2 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("variations.fields.active")} />
                  {t("variations.fields.active")}
                </label>
                {!variation && (
                  <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                    <Switch checked={isDefault} onCheckedChange={setIsDefault} aria-label={t("variations.fields.isDefault")} />
                    {t("variations.fields.isDefault")}
                  </label>
                )}
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={manageStock} onCheckedChange={setManageStock} aria-label={t("variations.fields.manageStock")} />
                  {t("variations.fields.manageStock")}
                </label>
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={allowBackorders} onCheckedChange={setAllowBackorders} aria-label={t("variations.fields.allowBackorders")} />
                  {t("variations.fields.allowBackorders")}
                </label>
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isVirtual} onCheckedChange={setIsVirtual} aria-label={t("variations.fields.isVirtual")} />
                  {t("variations.fields.isVirtual")}
                </label>
              </div>
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={saveMutation.isPending}><X className="size-4" />{tc("actions.cancel")}</Button>
            </DialogClose>
            <Button type="submit" disabled={saveMutation.isPending || !canSubmit}>
              <Check className="size-4" />{saveMutation.isPending ? tc("feedback.saving") : variation ? tc("actions.saveChanges") : t("variations.actions.add")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Delete variation
// ───────────────────────────────────────────────────────────────────────────

export function DeleteVariationDialog({
  productId,
  state,
  onClose,
}: {
  productId: string;
  state: VarEditor;
  onClose: () => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "delete";
  const variation = state.mode === "delete" ? state.variation : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: () => deleteVariation(productId, variation!.id),
    onSuccess: () => {
      toast.success(t("variations.deleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "variations", productId] });
      onClose();
    },
    onError: err => toast.error(t("variations.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("variations.actions.delete")}</DialogTitle>
          <DialogDescription>{t("variations.deleteDesc", { sku: variation?.sku ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}><X className="size-4" />{tc("actions.cancel")}</Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => deleteMutation.mutate()} disabled={deleteMutation.isPending}>
            <Trash2 className="size-4" />{deleteMutation.isPending ? tc("feedback.deleting") : t("variations.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Codes dialog (per variation)
// ───────────────────────────────────────────────────────────────────────────

export function CodesDialog({
  productId,
  state,
  onClose,
  canEdit,
}: {
  productId: string;
  state: VarEditor;
  onClose: () => void;
  canEdit: boolean;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const isOpen = state.mode === "codes";
  const variation = state.mode === "codes" ? state.variation : undefined;
  const queryClient = useQueryClient();

  const [codeType, setCodeType] = useState<ProductCodeType>("EAN");
  const [code, setCode] = useState("");
  const [supplierId, setSupplierId] = useState("");

  useEffect(() => {
    if (isOpen) { setCodeType("EAN"); setCode(""); setSupplierId(""); }
  }, [isOpen]);

  const codesQuery = useQuery({
    queryKey: ["catalog", "codes", productId, variation?.id],
    queryFn: () => getProductCodes(productId, variation!.id),
    enabled: isOpen && !!variation,
  });

  const addMutation = useMutation({
    mutationFn: () => addProductCode(productId, variation!.id, {
      codeType,
      code: code.trim(),
      supplierId: codeType === "SupplierCode" ? (supplierId.trim() || null) : null,
    }),
    onSuccess: () => {
      toast.success(t("codes.added"));
      setCode(""); setSupplierId("");
      queryClient.invalidateQueries({ queryKey: ["catalog", "codes", productId, variation?.id] });
    },
    onError: err => toast.error(t("codes.addFailed"), { description: describe(err) }),
  });

  const removeMutation = useMutation({
    mutationFn: (codeId: string) => removeProductCode(productId, variation!.id, codeId),
    onSuccess: () => {
      toast.success(t("codes.removed"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "codes", productId, variation?.id] });
    },
    onError: err => toast.error(t("codes.removeFailed"), { description: describe(err) }),
  });

  const codes = codesQuery.data ?? [];
  const canAdd = !!code.trim() && (codeType !== "SupplierCode" || !!supplierId.trim());

  return (
    <Dialog open={isOpen} onOpenChange={o => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{t("codes.title")}</DialogTitle>
          <DialogDescription>{t("codes.desc", { sku: variation?.sku ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="mb-4 space-y-1.5">
            {codes.length === 0 ? (
              <p className="py-2 text-[13px] text-[var(--color-muted-foreground)]">{t("codes.empty")}</p>
            ) : (
              codes.map(c => (
                <div key={c.id} className="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-background)] px-3 py-2">
                  <span className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 text-[11px] font-semibold uppercase text-[var(--color-muted-foreground)]">
                    {c.codeType}
                  </span>
                  <code className="flex-1 font-mono text-[13px] text-[var(--color-foreground)]">{c.code}</code>
                  {c.isPrimary && <Star className="size-3.5 fill-[var(--color-primary)] text-[var(--color-primary)]" />}
                  {canEdit && (
                    <button
                      type="button"
                      onClick={() => removeMutation.mutate(c.id)}
                      disabled={removeMutation.isPending}
                      className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"
                      aria-label={t("codes.remove")}
                    >
                      <Trash2 className="size-4" />
                    </button>
                  )}
                </div>
              ))
            )}
          </div>

          {canEdit && (
            <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] p-3">
              <FormGrid>
                <Field id="code-type" span={4} label={t("codes.fields.type")}>
                  <Combobox
                    id="code-type"
                    label={t("codes.fields.type")}
                    value={codeType}
                    onChange={v => setCodeType((v ?? "EAN") as ProductCodeType)}
                    options={PRODUCT_CODE_TYPES.map(ct => ({ value: ct, label: ct }))}
                  />
                </Field>
                <Field id="code-value" span={codeType === "SupplierCode" ? 4 : 8} label={t("codes.fields.code")} required>
                  <Input id="code-value" value={code} onChange={e => setCode(e.target.value)} placeholder="4548736130951" className="font-mono" maxLength={128} />
                </Field>
                {codeType === "SupplierCode" && (
                  <Field id="code-supplier" span={4} label={t("codes.fields.supplierId")} hint={t("codes.supplierHint")}>
                    <Input id="code-supplier" value={supplierId} onChange={e => setSupplierId(e.target.value)} placeholder="GUID" className="font-mono" />
                  </Field>
                )}
                <div className="col-span-1 sm:col-span-12">
                  <Button
                    type="button"
                    onClick={() => addMutation.mutate()}
                    disabled={!canAdd || addMutation.isPending}
                    className={cn("h-9 gap-1.5", !canAdd && "opacity-60")}
                  >
                    <Plus className="size-4" />
                    {t("codes.actions.add")}
                  </Button>
                </div>
              </FormGrid>
            </div>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline"><X className="size-4" />{tc("actions.close")}</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
