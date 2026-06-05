import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Hash, Layers, Plus, Star, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  addProductCode,
  addVariation,
  deleteVariation,
  getProductById,
  getProductCodes,
  getVariations,
  removeProductCode,
  updateVariation,
  PRODUCT_CODE_TYPES,
  type AddVariationInput,
  type ProductCodeType,
  type UpdateVariationInput,
  type VariationDto,
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

const WEIGHT_UNITS = ["KG", "G", "LB", "OZ"];

type VarEditor =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; variation: VariationDto }
  | { mode: "delete"; variation: VariationDto }
  | { mode: "codes"; variation: VariationDto };

// ── Cell templates ───────────────────────────────────────────────────────────
function VarSkuCell(row: VariationDto) {
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
function VarDescCell(row: VariationDto) {
  return <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.description || "—"}</span>;
}

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
    { field: "sku", headerText: t("variations.fields.sku"), template: VarSkuCell as any, minWidth: 200 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "description", headerText: t("variations.fields.description"), template: VarDescCell as any, minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "isActive", headerText: t("variations.fields.active"), template: StatusActiveCell as any, width: 120, allowSorting: false, textAlign: "Center" },
  ], [t, StatusActiveCell]);

  const statusTone = product?.status === "Active" ? "success" : product?.status === "Draft" ? "default" : "warning";

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

      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Layers className="size-4 text-[var(--color-muted-foreground)]" />
            <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("variations.title")}</h2>
            <span className="rounded-full bg-[var(--color-muted)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-muted-foreground)]">
              {variations.length}
            </span>
          </div>
          <Button
            perm={P.catalog.products.update}
            onClick={() => setEditor({ mode: "create" })}
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
          >
            <Plus className="size-4" />
            {t("variations.actions.add")}
          </Button>
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
      </div>

      <VariationEditorDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteVariationDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <CodesDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} canEdit={can(P.catalog.products.update)} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Variation editor (add / edit)
// ───────────────────────────────────────────────────────────────────────────

function VariationEditorDialog({
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
              <Button type="button" variant="outline" disabled={saveMutation.isPending}>{tc("actions.cancel")}</Button>
            </DialogClose>
            <Button type="submit" disabled={saveMutation.isPending || !canSubmit}>
              {saveMutation.isPending ? tc("feedback.saving") : variation ? tc("actions.saveChanges") : t("variations.actions.add")}
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

function DeleteVariationDialog({
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
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>{tc("actions.cancel")}</Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => deleteMutation.mutate()} disabled={deleteMutation.isPending}>
            {deleteMutation.isPending ? tc("feedback.deleting") : t("variations.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Codes dialog (per variation)
// ───────────────────────────────────────────────────────────────────────────

function CodesDialog({
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
            <Button type="button" variant="outline">{tc("actions.close")}</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
