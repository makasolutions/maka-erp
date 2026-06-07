import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft, Boxes, Check, ChevronDown, FileText, Hash, ImageIcon, Layers, Megaphone, Pencil, Plus, SlidersHorizontal, Star, Trash2,
} from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  addBundleItem, addPriceListItem, addProductCode, addProductImage, changeProductType, createBrand, createCategory, createProduct, getBundleItems,
  getCategoryTree, getDefaultVariation, getEffectivePrice, getPriceListById, getPriceLists, getProductById, getProductCodes,
  getShippingClasses, getTaxRates, getVariations, removeBundleItem, removeProductCode, searchBrands,
  searchProducts, setProductCategories, updatePriceListItem, updateProduct,
  type BrandDto, type CategoryDto, type ProductType, type VariationDto,
} from "@/api/catalog";
import { PRODUCT_CODE_TYPES, validateProductCode, type ProductCodeType } from "@/lib/product-codes";
import {
  GenerateVariationsButton, ProductAttributesTab, ProductImagesTab, ProductTagsTab,
  VariationEditorDialog, DeleteVariationDialog,
  CodesDialog, type VarEditor,
} from "@/pages/catalog/product-detail";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Combobox, EntityStatusBadge, Field, FormGrid } from "@/components/list";
import { MakaCurrencyInput, MakaRichTextEditor } from "@/components/maka";
import { cn } from "@/lib/cn";
import { deriveBaseFromDefault, describe, formatMoney, slugify, toTaxIncluded } from "@/lib/list-helpers";
import { buildBundleCollage } from "@/lib/bundle-collage";
import { useFileUpload } from "@/hooks/use-file-upload";
import { getFileMetadata, Visibility } from "@/api/files";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const PRODUCT_TYPES: ProductType[] = ["Simple", "Variable", "Bundle", "Service"];
const WEIGHT_UNITS = ["KG", "G", "LB", "OZ"];

type StepId = "general" | "description" | "attributes" | "media" | "specs";

const STEPS: { id: StepId; icon: typeof Layers }[] = [
  { id: "general", icon: FileText },
  { id: "description", icon: FileText },
  { id: "attributes", icon: SlidersHorizontal },
  { id: "media", icon: ImageIcon },
  { id: "specs", icon: Layers },
];

export function ProductFormPage() {
  const { productId: routeId } = useParams();
  const navigate = useNavigate();
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const queryClient = useQueryClient();
  const canEdit = can(P.catalog.products.update);

  const isNew = !routeId || routeId === "new";
  const [productId, setProductId] = useState<string | null>(isNew ? null : routeId!);
  const [step, setStep] = useState(0);

  // ── General fields ──
  const [type, setType] = useState<ProductType>("Simple");
  const [name, setName] = useState("");
  const [brandId, setBrandId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [sku, setSku] = useState("");

  // ── Descriptions ──
  const [shortDescription, setShortDescription] = useState("");
  const [description, setDescription] = useState("");

  // ── Specs / shipping ──
  const [technicalSpecs, setTechnicalSpecs] = useState("");
  const [specs, setSpecs] = useState("");
  const [taxRateId, setTaxRateId] = useState<string | null>(null);
  const [shippingClassId, setShippingClassId] = useState<string | null>(null);
  const [weight, setWeight] = useState("");
  const [weightUnit, setWeightUnit] = useState("KG");
  const [dimL, setDimL] = useState("");
  const [dimW, setDimW] = useState("");
  const [dimH, setDimH] = useState("");
  const [dimUnit, setDimUnit] = useState("CM");

  const detailQuery = useQuery({
    queryKey: ["catalog", "products", "detail", productId],
    queryFn: () => getProductById(productId!),
    enabled: !!productId,
  });

  // Hydrate the SKU field from the default variation (edit mode).
  const defaultVarQuery = useQuery({
    queryKey: ["catalog", "default-variation", productId],
    queryFn: () => getDefaultVariation(productId!),
    enabled: !!productId,
  });
  useEffect(() => {
    if (defaultVarQuery.data?.sku) setSku(defaultVarQuery.data.sku);
  }, [defaultVarQuery.data]);

  // Hydrate form from the loaded product (edit / after create).
  useEffect(() => {
    const p = detailQuery.data;
    if (!p) return;
    setType(p.type);
    setName(p.name);
    setBrandId(p.brandId ?? null);
    setCategoryId(p.categories.find((c) => c.isPrimary)?.id ?? p.categories[0]?.id ?? null);
    setShortDescription(p.shortDescription ?? "");
    setDescription(p.description ?? "");
    setTechnicalSpecs(p.technicalSpecs ?? "");
    setSpecs(p.specs ?? "");
    setTaxRateId(p.taxRateId ?? null);
    setShippingClassId(p.shippingClassId ?? null);
    setWeight(p.weight != null ? String(p.weight) : "");
    setWeightUnit(p.weightUnit ?? "KG");
    setDimL(p.dimensionLength != null ? String(p.dimensionLength) : "");
    setDimW(p.dimensionWidth != null ? String(p.dimensionWidth) : "");
    setDimH(p.dimensionHeight != null ? String(p.dimensionHeight) : "");
    setDimUnit(p.dimensionUnit ?? "CM");
  }, [detailQuery.data]);

  const specsValid = useMemo(() => {
    if (!specs.trim()) return true;
    try { JSON.parse(specs); return true; } catch { return false; }
  }, [specs]);

  const createMutation = useMutation({
    mutationFn: async () => {
      const id = await createProduct({
        name: name.trim(), type, brandId, defaultSku: sku.trim() || null,
      });
      if (categoryId) await setProductCategories({ productId: id, categories: [{ categoryId, isPrimary: true }] });
      return id;
    },
    onSuccess: (id) => {
      toast.success(tc("feedback.created"));
      setProductId(id);
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      // Stay on the General step (replace URL) so the codes section appears
      // in place now that the product (and its default variation) exists.
      navigate(`/catalog/products/${id}`, { replace: true });
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!productId) return;
      await updateProduct({
        productId, name: name.trim(),
        shortDescription: shortDescription.trim() || null,
        description: description.trim() || null,
        technicalSpecs: technicalSpecs.trim() || null,
        brandId, taxRateId, shippingClassId,
        isVirtual: type === "Service", isDownloadable: false, isPublic: detailQuery.data?.isPublic ?? false,
        weight: weight ? Number(weight) : null, weightUnit,
        dimensionLength: dimL ? Number(dimL) : null,
        dimensionWidth: dimW ? Number(dimW) : null,
        dimensionHeight: dimH ? Number(dimH) : null,
        dimensionUnit: dimUnit,
        seoTitle: detailQuery.data?.seoTitle ?? null,
        seoDescription: detailQuery.data?.seoDescription ?? null,
        seoKeywords: detailQuery.data?.seoKeywords ?? null,
        specs: specs.trim() || null,
      });
      if (categoryId) await setProductCategories({ productId, categories: [{ categoryId, isPrimary: true }] });
    },
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products", "detail", productId] });
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
    },
    onError: (err) => toast.error(tc("feedback.updateFailed"), { description: describe(err) }),
  });

  const step1Valid = !!name.trim() && !!sku.trim();

  const goNext = () => {
    if (step === 0 && isNew && !productId) { createMutation.mutate(); return; }
    if (step === 0 && productId) saveMutation.mutate();
    if (step === 1 && productId) saveMutation.mutate();
    if (step === 4 && productId) saveMutation.mutate();
    setStep((s) => Math.min(STEPS.length - 1, s + 1));
  };

  const currentStep = STEPS[step].id;

  // Step-3 label adapts to the product type: Variable → "Attributes & variations",
  // Bundle → "Bundle items", Simple/Service → "Attributes" (no variations).
  const stepLabel = (id: StepId) =>
    id !== "attributes"
      ? t(`wizard.steps.${id}`)
      : type === "Variable" ? t("wizard.steps.attributes")
      : type === "Bundle" ? t("wizard.steps.bundle")
      : t("wizard.steps.attributesOnly");

  return (
    <div className="space-y-4 sm:space-y-6">
      <div>
        <Link to="/catalog/products" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]">
          <ArrowLeft className="size-4" />
          {t("variations.backToProducts")}
        </Link>
        <h1 className="font-display text-[22px] font-semibold leading-tight text-[var(--color-foreground)]">
          {isNew && !productId ? t("wizard.createTitle") : (detailQuery.data?.name ?? t("wizard.editTitle"))}
        </h1>
      </div>

      {/* Stepper — mobile-first horizontal scroll */}
      <ol className="flex gap-1 overflow-x-auto pb-1" aria-label={t("wizard.step")}>
        {STEPS.map((s, i) => {
          const Icon = s.icon;
          const active = i === step;
          const done = i < step;
          const reachable = !!productId || i === 0;
          return (
            <li key={s.id} className="flex-1 min-w-[120px]">
              <button
                type="button"
                disabled={!reachable}
                onClick={() => reachable && setStep(i)}
                className={cn(
                  "flex w-full items-center gap-2 rounded-lg border px-3 py-2 text-left text-[12.5px] font-semibold transition-colors",
                  active ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-foreground)]"
                    : done ? "border-[var(--color-border)] text-[var(--color-foreground)]"
                    : "border-[var(--color-border)] text-[var(--color-muted-foreground)]",
                  !reachable && "opacity-50",
                )}
              >
                <span className={cn("grid size-5 shrink-0 place-items-center rounded-full text-[10px]",
                  done ? "bg-[var(--color-success)] text-white" : active ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]" : "bg-[var(--color-muted)]")}>
                  {done ? <Check className="size-3" /> : i + 1}
                </span>
                <span className="flex items-center gap-1 truncate"><Icon className="size-3.5" />{stepLabel(s.id)}</span>
              </button>
            </li>
          );
        })}
      </ol>

      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4 sm:p-6">
        {currentStep === "general" && (
          <GeneralStep
            isNew={isNew && !productId} productId={productId} canEdit={canEdit}
            type={type} setType={setType} name={name} setName={setName}
            brandId={brandId} setBrandId={setBrandId} categoryId={categoryId} setCategoryId={setCategoryId}
            sku={sku} setSku={setSku}
          />
        )}
        {currentStep === "description" && (
          <DescriptionStep
            shortDescription={shortDescription} setShortDescription={setShortDescription}
            description={description} setDescription={setDescription}
          />
        )}
        {currentStep === "attributes" && productId && (
          type === "Variable" ? <VariationsStep productId={productId} canEdit={canEdit} categoryId={categoryId} />
          : type === "Bundle" ? <BundleStep productId={productId} canEdit={canEdit} />
          : <AttributesOnlyStep productId={productId} canEdit={canEdit} categoryId={categoryId} />
        )}
        {currentStep === "media" && productId && (
          <div className="space-y-8">
            <ProductImagesTab productId={productId} canEdit={canEdit} />
            <div className="border-t border-[var(--color-border)] pt-6">
              <ProductTagsTab productId={productId} canEdit={canEdit} />
            </div>
          </div>
        )}
        {currentStep === "specs" && (
          <div className="space-y-8">
            <SpecsStep
              technicalSpecs={technicalSpecs} setTechnicalSpecs={setTechnicalSpecs}
              specs={specs} setSpecs={setSpecs} specsValid={specsValid}
              taxRateId={taxRateId} setTaxRateId={setTaxRateId}
              shippingClassId={shippingClassId} setShippingClassId={setShippingClassId}
              weight={weight} setWeight={setWeight} weightUnit={weightUnit} setWeightUnit={setWeightUnit}
              dimL={dimL} setDimL={setDimL} dimW={dimW} setDimW={setDimW} dimH={dimH} setDimH={setDimH}
              dimUnit={dimUnit} setDimUnit={setDimUnit}
            />
            {productId && type !== "Variable" && (
              <div className="border-t border-[var(--color-border)] pt-6">
                <PriceListsInputs productId={productId} canEdit={canEdit} />
              </div>
            )}
          </div>
        )}
      </div>

      {/* Footer nav */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <Button variant="ghost" onClick={() => navigate("/catalog/products")}>
            {t("wizard.cancel")}
          </Button>
          <Button variant="outline" disabled={step === 0} onClick={() => setStep((s) => Math.max(0, s - 1))}>
            {t("wizard.back")}
          </Button>
        </div>
        <div className="flex items-center gap-2">
          {!isNew || productId ? (
            <Button variant="outline" onClick={() => saveMutation.mutate()} disabled={!canEdit || saveMutation.isPending}>
              {saveMutation.isPending ? tc("feedback.saving") : t("wizard.saveDraft")}
            </Button>
          ) : null}
          {step < STEPS.length - 1 ? (
            <Button onClick={goNext} disabled={(step === 0 && !step1Valid) || createMutation.isPending}>
              {createMutation.isPending ? t("wizard.creating") : t("wizard.next")}
            </Button>
          ) : (
            <Button onClick={() => { saveMutation.mutate(); navigate("/catalog/products"); }} disabled={!canEdit}>
              {t("wizard.finish")}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Createable combobox (brand / category inline-create)
// ───────────────────────────────────────────────────────────────────────────

function CreateableCombobox({
  id, label, value, onChange, options, onCreate, placeholder, invalidateKey,
}: {
  id: string; label: string; value: string | null;
  onChange: (v: string | null) => void;
  options: { value: string; label: string }[];
  onCreate: (name: string) => Promise<string>;
  placeholder: string;
  invalidateKey: readonly unknown[];
}) {
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();

  const handleCreate = async (query: string) => {
    try {
      const newId = await onCreate(query.trim());
      await queryClient.invalidateQueries({ queryKey: invalidateKey });
      onChange(newId);
      toast.success(t("inlineCreate.created"));
    } catch (e) {
      toast.error(t("inlineCreate.failed"), { description: describe(e) });
    }
  };

  return (
    <Combobox
      id={id} label={label} value={value} onChange={onChange}
      options={options} searchable clearable placeholder={placeholder}
      onCreate={(q) => { void handleCreate(q); }}
      createLabel={t("inlineCreate.createShort", "Crear")}
    />
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Step 1 — General + codes
// ───────────────────────────────────────────────────────────────────────────

function GeneralStep({
  isNew, productId, canEdit, type, setType, name, setName, brandId, setBrandId,
  categoryId, setCategoryId, sku, setSku,
}: {
  isNew: boolean; productId: string | null; canEdit: boolean;
  type: ProductType; setType: (v: ProductType) => void;
  name: string; setName: (v: string) => void;
  brandId: string | null; setBrandId: (v: string | null) => void;
  categoryId: string | null; setCategoryId: (v: string | null) => void;
  sku: string; setSku: (v: string) => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  // Convert Simple ↔ Variable (only those two; not allowed when creating).
  const convertTarget: ProductType | null = type === "Simple" ? "Variable" : type === "Variable" ? "Simple" : null;
  const convertM = useMutation({
    mutationFn: (target: ProductType) => changeProductType(productId!, target),
    onSuccess: (_id, target) => {
      toast.success(tc("feedback.updated"));
      setType(target);
      queryClient.invalidateQueries({ queryKey: ["catalog", "products", "detail", productId] });
      queryClient.invalidateQueries({ queryKey: ["catalog", "variations", productId] });
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "list"],
    queryFn: () => searchBrands({ pageSize: 200, sort: "name" }),
    placeholderData: keepPreviousData,
  });
  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: () => getCategoryTree(),
    placeholderData: keepPreviousData,
  });

  const brandOptions = (brandsQuery.data?.items ?? []).map((b: BrandDto) => ({ value: b.id, label: b.name }));
  const flatCats = useMemo(() => flatten(categoriesQuery.data ?? []), [categoriesQuery.data]);
  const catOptions = flatCats.map((c) => ({ value: c.id, label: c.label }));

  const typeOptions = PRODUCT_TYPES.map((tp) => ({ value: tp, label: t(`products.types.${tp}`, tp) }));
  const skuTooLong = sku.trim().length > 64;

  return (
    <div className="space-y-6">
      <FormGrid>
        <Field id="p-sku" span={3} label="SKU" required hint={isNew ? undefined : t("variations.skuImmutable", "")}>
          <Input id="p-sku" value={sku} onChange={(e) => setSku(e.target.value.toUpperCase())}
            maxLength={64} required disabled={!isNew} placeholder="SONY-FX3" className="font-mono uppercase"
            aria-invalid={skuTooLong || (isNew && !sku.trim())} />
          {skuTooLong && <p className="mt-1 text-[11.5px] text-[var(--color-destructive)]">{t("codes.errors.tooLong")}</p>}
          {isNew && !sku.trim() && <p className="mt-1 text-[11.5px] text-[var(--color-destructive)]">{t("codes.skuRequired")}</p>}
        </Field>
        <Field id="p-type" span={3} label={t("products.fields.type")} required hint={!isNew ? t("wizard.typeHint.immutable") : undefined}>
          <Combobox id="p-type" label={t("products.fields.type")} value={type}
            onChange={(v) => v && setType(v as ProductType)} options={typeOptions} disabled={!isNew} />
          {!isNew && productId && canEdit && convertTarget && (
            <button type="button" onClick={() => convertM.mutate(convertTarget)} disabled={convertM.isPending}
              className="mt-1 text-[11.5px] font-medium text-[var(--color-primary)] hover:underline disabled:opacity-60">
              {convertM.isPending ? tc("feedback.saving") : t("products.actions.convertTo", { type: t(`products.types.${convertTarget}`) })}
            </button>
          )}
        </Field>
        <Field id="p-name" span={6} label={t("products.fields.name")} required>
          <Input id="p-name" value={name} onChange={(e) => setName(e.target.value)}
            maxLength={200} required autoFocus placeholder={t("products.namePlaceholder", "")} />
        </Field>
        <Field id="p-cat" span={6} label={t("products.fields.category")}>
          <CreateableCombobox id="p-cat" label={t("products.fields.category")} value={categoryId}
            onChange={setCategoryId} options={catOptions} placeholder={t("products.allCategories", "")}
            invalidateKey={["catalog", "categories", "tree"]}
            onCreate={(n) => createCategory({ name: n, slug: slugify(n), isActive: true })} />
        </Field>
        <Field id="p-brand" span={6} label={t("products.fields.brand")}>
          <CreateableCombobox id="p-brand" label={t("products.fields.brand")} value={brandId}
            onChange={setBrandId} options={brandOptions} placeholder={t("products.allBrands", "")}
            invalidateKey={["catalog", "brands", "list"]}
            onCreate={(n) => createBrand({ name: n, slug: slugify(n), isActive: true })} />
        </Field>
      </FormGrid>

      {/* Two 50/50 columns: prices per list (left) + codes (right), both vertical. */}
      {productId && type !== "Variable" && (
        <div className="grid gap-6 border-t border-[var(--color-border)] pt-6 lg:grid-cols-2">
          <PriceListsInputs productId={productId} canEdit={canEdit} />
          <DefaultVariationCodes productId={productId} canEdit={canEdit} />
        </div>
      )}
      {productId && type === "Variable" && (
        <p className="border-t border-[var(--color-border)] pt-6 text-[12.5px] text-[var(--color-muted-foreground)]">
          {t("wizard.variableOnly")}
        </p>
      )}
    </div>
  );
}

type FlatCat = { id: string; label: string };
function flatten(nodes: CategoryDto[], depth = 0, acc: FlatCat[] = []): FlatCat[] {
  for (const n of nodes) {
    acc.push({ id: n.id, label: `${"— ".repeat(depth)}${n.name}` });
    if (n.children?.length) flatten(n.children, depth + 1, acc);
  }
  return acc;
}

// ── Money control with VAT: base ↔ IVA-included (19%) ──
// Three inputs sharing one field width: [base] [19%] [IVA incl.]. Editing the
// base computes the VAT-included value; editing the VAT-included computes the
// base. The stored value is always the base (price sin IVA).
const IVA_RATE = 0.19;
function PriceWithTax({ id, value, onChange, disabled }: {
  id: string; value: string; onChange: (base: string) => void; disabled?: boolean;
}) {
  const { t } = useTranslation("catalog");
  const baseNum = value === "" ? null : Number(value);
  const ivaNum = baseNum == null ? null : Math.round(baseNum * (1 + IVA_RATE));

  return (
    <div>
      <div className="flex items-stretch gap-1">
        <div className="min-w-0 flex-1">
          <MakaCurrencyInput id={id} value={baseNum} disabled={disabled}
            onChange={(n) => onChange(n == null ? "" : String(n))} ariaLabel={t("priceLists.base")} />
        </div>
        <span aria-hidden className="grid w-10 shrink-0 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] text-[11px] font-semibold text-[var(--color-muted-foreground)]">
          19%
        </span>
        <div className="min-w-0 flex-1">
          <MakaCurrencyInput value={ivaNum} disabled={disabled}
            onChange={(n) => onChange(n == null ? "" : String(Math.round(n / (1 + IVA_RATE))))} ariaLabel={t("priceLists.withTax")} />
        </div>
      </div>
      <div className="mt-0.5 flex gap-1 text-[10px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
        <span className="flex-1">{t("priceLists.base")}</span>
        <span className="w-10 shrink-0 text-center">{t("priceLists.vatLabel")}</span>
        <span className="flex-1">{t("priceLists.withTax")}</span>
      </div>
    </div>
  );
}

// ── Campaign banner: shown when the product's default variation is in a running campaign ──
function CampaignBanner({ variationId }: { variationId: string }) {
  const { t } = useTranslation("catalog");
  const q = useQuery({
    queryKey: ["catalog", "effective-price", variationId, "Retail"],
    queryFn: () => getEffectivePrice(variationId, "Retail"),
    enabled: !!variationId,
    retry: false,
  });
  const p = q.data;
  if (!p?.isCampaign) return null;
  const fmtD = (s?: string | null) => s ? new Date(s).toLocaleDateString("es-CO", { year: "numeric", month: "short", day: "numeric" }) : "—";
  return (
    <div className="flex flex-wrap items-center gap-2 rounded-lg border border-[var(--color-warning)] bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] px-3 py-2 text-[12.5px]">
      <Megaphone className="size-4 text-[var(--color-warning)]" />
      <span className="font-semibold text-[var(--color-foreground)]">{t("campaigns.inCampaign")}: {p.priceListName}</span>
      <span className="text-[var(--color-muted-foreground)]">{fmtD(p.campaignFrom)} → {fmtD(p.campaignTo)}</span>
      <span className="font-semibold tabular-nums text-[var(--color-foreground)]">{formatMoney(toTaxIncluded(p.effectivePrice))}</span>
      <span className="text-[10.5px] uppercase text-[var(--color-muted-foreground)]">{t("priceLists.withTaxIncluded")}</span>
    </div>
  );
}

// ── Price per list for the default variation (Fase 3) ──
// The default list drives the others: editing the base price recomputes the
// derived lists (suggested, rounded, editable). Each derived input the user
// edits becomes a manual override.
function PriceListsInputs({ productId, canEdit, variationId: variationIdProp, hideTitle }: {
  productId: string; canEdit: boolean; variationId?: string; hideTitle?: boolean;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const defVarQuery = useQuery({
    queryKey: ["catalog", "default-variation", productId],
    queryFn: () => getDefaultVariation(productId),
    enabled: !variationIdProp,
  });
  const variationId = variationIdProp ?? defVarQuery.data?.id;

  const listsQuery = useQuery({
    queryKey: ["catalog", "price-lists", "active"],
    queryFn: () => getPriceLists({ pageSize: 200, isActive: true, sort: "name", kind: "Segment" }),
  });
  const lists = useMemo(() => {
    const items = listsQuery.data?.items ?? [];
    return [...items].sort((a, b) => Number(b.isDefault) - Number(a.isDefault) || a.name.localeCompare(b.name));
  }, [listsQuery.data]);
  const defaultList = lists.find((l) => l.isDefault);
  const listIdsKey = lists.map((l) => l.id).join(",");

  // Current item price + itemId per list for this variation.
  const pricesQuery = useQuery({
    queryKey: ["catalog", "product-list-prices", productId, variationId, listIdsKey],
    enabled: !!variationId && lists.length > 0,
    queryFn: async () => {
      const details = await Promise.all(lists.map((l) => getPriceListById(l.id)));
      const out: Record<string, { price: number | null; itemId: string | null; override: boolean }> = {};
      details.forEach((d, i) => {
        const item = d.items.find((it) => it.variationId === variationId);
        out[lists[i].id] = { price: item?.price ?? null, itemId: item?.id ?? null, override: item?.isManualOverride ?? false };
      });
      return out;
    },
  });

  const [values, setValues] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  useEffect(() => {
    if (pricesQuery.data) {
      const v: Record<string, string> = {};
      const tt: Record<string, boolean> = {};
      for (const [id, info] of Object.entries(pricesQuery.data)) {
        v[id] = info.price != null ? String(info.price) : "";
        tt[id] = info.override;
      }
      setValues(v);
      setTouched(tt);
    }
  }, [pricesQuery.data]);

  const setBase = (raw: string) => {
    setValues((prev) => {
      const next = { ...prev };
      if (defaultList) next[defaultList.id] = raw;
      const base = Number(raw);
      // Derived lists are computed from the default list's VAT-included value,
      // rounded only when the list has RoundEnabled.
      if (defaultList && raw && !Number.isNaN(base)) {
        for (const l of lists) {
          if (l.isDefault || l.adjustmentPercent == null) continue;
          if (touched[l.id]) continue; // respect manual override
          next[l.id] = String(deriveBaseFromDefault(base, l.adjustmentPercent, l.roundEnabled));
        }
      }
      return next;
    });
  };

  const defaultPriceMissing = !!defaultList && !(values[defaultList.id] ?? "").trim();

  const saveM = useMutation({
    mutationFn: async () => {
      if (!variationId) return;
      const info = pricesQuery.data ?? {};
      for (const l of lists) {
        const raw = values[l.id];
        if (raw == null || raw === "") continue;
        const price = Number(raw);
        if (Number.isNaN(price)) continue;
        const cur = info[l.id];
        if (cur?.itemId) {
          if (cur.price !== price) await updatePriceListItem(l.id, cur.itemId, { price });
        } else {
          await addPriceListItem(l.id, { variationId, price });
        }
      }
    },
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "product-list-prices", productId] });
      queryClient.invalidateQueries({ queryKey: ["catalog", "price-lists"] });
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  if (!variationId) return null;

  // No price lists yet — remind what it's for and offer to create one.
  if (lists.length === 0) {
    return (
      <div className="space-y-2 rounded-lg border border-dashed border-[var(--color-border)] p-4">
        <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("priceLists.productPricesTitle")}</h3>
        <p className="text-[12px] text-[var(--color-muted-foreground)]">{t("priceLists.noListsHint")}</p>
        {canEdit && (
          <Button type="button" variant="outline" onClick={() => navigate("/catalog/price-lists")}>
            <Plus className="size-4" />{t("priceLists.createFirst")}
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <CampaignBanner variationId={variationId} />
      {!hideTitle && (
        <>
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("priceLists.productPricesTitle")}</h3>
            {!defaultList && <span className="text-[12px] text-[var(--color-warning)]">{t("priceLists.noDefault")}</span>}
          </div>
          <p className="text-[12px] text-[var(--color-muted-foreground)]">{t("priceLists.productPricesHint")}</p>
        </>
      )}
      <FormGrid>
        {lists.map((l) => (
          <Field key={l.id} id={`plp-${l.id}`} span={12}
            label={`${l.name}${l.isDefault ? " ★" : l.adjustmentPercent != null ? ` (${l.adjustmentPercent > 0 ? "+" : ""}${l.adjustmentPercent}%)` : ""}`}>
            <PriceWithTax id={`plp-${l.id}`} value={values[l.id] ?? ""} disabled={!canEdit}
              onChange={(base) => {
                if (l.isDefault) { setBase(base); }
                else { setValues((p) => ({ ...p, [l.id]: base })); setTouched((p) => ({ ...p, [l.id]: true })); }
              }} />
          </Field>
        ))}
      </FormGrid>
      {canEdit && (
        <div className="flex items-center justify-end gap-3">
          {defaultPriceMissing && (
            <span className="text-[11.5px] text-[var(--color-destructive)]">{t("priceLists.defaultRequired")}</span>
          )}
          <Button type="button" variant="outline" onClick={() => saveM.mutate()}
            disabled={saveM.isPending || defaultPriceMissing}>
            {saveM.isPending ? tc("feedback.saving") : t("priceLists.savePrices")}
          </Button>
        </div>
      )}
    </div>
  );
}

// ── Codes manager for the default variation (SKU + EAN/UPC/GTIN/ISBN…) ──
function DefaultVariationCodes({ productId, canEdit }: { productId: string; canEdit: boolean }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const variationQuery = useQuery({
    queryKey: ["catalog", "default-variation", productId],
    queryFn: () => getDefaultVariation(productId),
  });
  const variationId = variationQuery.data?.id;

  const codesQuery = useQuery({
    queryKey: ["catalog", "codes", productId, variationId],
    queryFn: () => getProductCodes(productId, variationId!),
    enabled: !!variationId,
  });

  const [codeType, setCodeType] = useState<ProductCodeType>("EAN");
  const [codeValue, setCodeValue] = useState("");

  const validation = codeValue ? validateProductCode(codeType, codeValue) : { valid: false };
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["catalog", "codes", productId, variationId] });

  const addMutation = useMutation({
    mutationFn: () => addProductCode(productId, variationId!, { codeType, code: codeValue.trim(), isPrimary: false }),
    onSuccess: () => { setCodeValue(""); invalidate(); },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });
  const removeMutation = useMutation({
    mutationFn: (codeId: string) => removeProductCode(productId, variationId!, codeId),
    onSuccess: invalidate,
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  const codes = codesQuery.data ?? [];
  const typeOptions = PRODUCT_CODE_TYPES.filter((x) => x !== "SKU").map((x) => ({ value: x, label: x }));

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <Hash className="size-4 text-[var(--color-muted-foreground)]" />
        <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("codes.title")}</h3>
      </div>

      {variationQuery.data?.sku && (
        <div className="flex items-center gap-2 text-[12.5px]">
          <span className="rounded bg-[var(--color-primary)] px-1.5 py-0.5 text-[10px] font-semibold text-[var(--color-primary-foreground)]">SKU</span>
          <code className="font-mono text-[var(--color-foreground)]">{variationQuery.data.sku}</code>
        </div>
      )}

      {codes.length > 0 && (
        <ul className="flex flex-wrap gap-2">
          {codes.filter((c) => c.codeType !== "SKU").map((c) => (
            <li key={c.id} className="flex items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2.5 py-1.5 text-[12.5px]">
              <span className="font-semibold text-[var(--color-muted-foreground)]">{c.codeType}</span>
              <code className="font-mono text-[var(--color-foreground)]">{c.code}</code>
              {canEdit && (
                <button type="button" onClick={() => removeMutation.mutate(c.id)} aria-label={tc("actions.delete")}
                  className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
                  <Trash2 className="size-3.5" />
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canEdit && variationId && (
        <div className="flex flex-wrap items-end gap-2">
          <div className="w-32">
            <Combobox id="code-type" label={t("codes.type")} value={codeType}
              onChange={(v) => v && setCodeType(v as ProductCodeType)} options={typeOptions} />
          </div>
          <div className="grow">
            <Input value={codeValue} onChange={(e) => setCodeValue(e.target.value)} placeholder={t("codes.value")}
              className="font-mono" aria-invalid={!!codeValue && !validation.valid}
              onKeyDown={(e) => { if (e.key === "Enter" && validation.valid) { e.preventDefault(); addMutation.mutate(); } }} />
            {!!codeValue && !validation.valid && validation.messageKey && (
              <p className="mt-1 text-[11.5px] text-[var(--color-destructive)]">{t(validation.messageKey)}</p>
            )}
          </div>
          <Button type="button" variant="outline" disabled={!validation.valid || addMutation.isPending}
            onClick={() => addMutation.mutate()}>
            <Plus className="size-4" />{t("codes.add")}
          </Button>
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Step 2 — Descriptions
// ───────────────────────────────────────────────────────────────────────────

function DescriptionStep({
  shortDescription, setShortDescription, description, setDescription,
}: {
  shortDescription: string; setShortDescription: (v: string) => void;
  description: string; setDescription: (v: string) => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <FormGrid>
      <Field id="p-short" span={12} label={t("wizard.descShort")} hint={t("wizard.descShortHint")}>
        <MakaRichTextEditor id="p-short" value={shortDescription} onChange={setShortDescription}
          height={120} maxLength={500} placeholder={t("wizard.descShort")} />
      </Field>
      <Field id="p-long" span={12} label={t("wizard.descLong")} hint={t("wizard.descLongHint")}>
        <MakaRichTextEditor id="p-long" value={description} onChange={setDescription}
          height={260} maxLength={50000} placeholder={t("wizard.descLong")} />
      </Field>
    </FormGrid>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Step 3 — Attributes + variations (Variable only)
// ───────────────────────────────────────────────────────────────────────────

function VariationsStep({ productId, canEdit, categoryId }: { productId: string; canEdit: boolean; categoryId?: string | null }) {
  const { t } = useTranslation("catalog");
  const [editor, setEditor] = useState<VarEditor>({ mode: "closed" });

  const variationsQuery = useQuery({
    queryKey: ["catalog", "variations", productId],
    queryFn: () => getVariations(productId),
  });
  const variations = useMemo(() => (variationsQuery.data ?? []).filter((v) => !v.isDeleted), [variationsQuery.data]);

  return (
    <div className="space-y-8">
      <ProductAttributesTab productId={productId} canEdit={canEdit} categoryId={categoryId} />

      <div className="border-t border-[var(--color-border)] pt-6">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Layers className="size-4 text-[var(--color-muted-foreground)]" />
            <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("variations.title")}</h2>
            <span className="rounded-full bg-[var(--color-muted)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-muted-foreground)]">{variations.length}</span>
          </div>
          <div className="flex items-center gap-2">
            <GenerateVariationsButton productId={productId} disabled={!canEdit} />
            <Button perm={P.catalog.products.update} onClick={() => setEditor({ mode: "create" })}
              className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
              <Plus className="size-4" />{t("variations.actions.add")}
            </Button>
          </div>
        </div>

        {variations.length === 0 ? (
          <p className="text-[13px] text-[var(--color-muted-foreground)]">{t("variations.empty")}</p>
        ) : (
          <div className="space-y-2">
            {variations.map((v) => (
              <VariationAccordionItem key={v.id} productId={productId} canEdit={canEdit} variation={v}
                onEdit={() => setEditor({ mode: "edit", variation: v })}
                onDelete={() => setEditor({ mode: "delete", variation: v })}
                onCodes={() => setEditor({ mode: "codes", variation: v })} />
            ))}
          </div>
        )}
      </div>

      <VariationEditorDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteVariationDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <CodesDialog productId={productId} state={editor} onClose={() => setEditor({ mode: "closed" })} canEdit={canEdit} />
    </div>
  );
}

// ── Accordion item: a variation with its price lists inside (50% column) ──
function VariationAccordionItem({ productId, canEdit, variation, onEdit, onDelete, onCodes }: {
  productId: string; canEdit: boolean; variation: VariationDto;
  onEdit: () => void; onDelete: () => void; onCodes: () => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const [open, setOpen] = useState(false);
  const combo = variation.attributeValues?.map((av) => `${av.attributeName}: ${av.value}`).join(" · ");

  return (
    <div className="overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-background)]">
      <div className="flex items-center gap-2 px-3 py-2.5">
        <button type="button" onClick={() => setOpen((o) => !o)} className="flex min-w-0 flex-1 items-center gap-2 text-left">
          <ChevronDown className={cn("size-4 shrink-0 text-[var(--color-muted-foreground)] transition-transform", open && "rotate-180")} />
          <code className="font-mono text-[13px] text-[var(--color-foreground)]">{variation.sku}</code>
          {variation.isDefault && <Star className="size-3.5 fill-[var(--color-warning)] text-[var(--color-warning)]" />}
          {combo && <span className="truncate text-[12px] text-[var(--color-muted-foreground)]">· {combo}</span>}
          <EntityStatusBadge tone={variation.isActive ? "success" : "default"}>
            {variation.isActive ? tc("status.active") : tc("status.inactive")}
          </EntityStatusBadge>
        </button>
        {canEdit && (
          <div className="flex shrink-0 items-center gap-1">
            <button type="button" onClick={onCodes} aria-label={t("codes.manage")} title={t("codes.manage")}
              className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"><Hash className="size-4" /></button>
            <button type="button" onClick={onEdit} aria-label={tc("actions.edit")} title={tc("actions.edit")}
              className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"><Pencil className="size-4" /></button>
            <button type="button" onClick={onDelete} aria-label={tc("actions.delete")} title={tc("actions.delete")}
              className="grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
          </div>
        )}
      </div>
      {open && (
        <div className="border-t border-[var(--color-border)] bg-[var(--color-card)] p-3">
          <div className="lg:w-1/2">
            <PriceListsInputs productId={productId} canEdit={canEdit} variationId={variation.id} hideTitle />
          </div>
        </div>
      )}
    </div>
  );
}

// ── Precios por variación (productos Variable) ──
// ───────────────────────────────────────────────────────────────────────────
//  Step 3 (Simple / Service) — attributes only (no variations)
// ───────────────────────────────────────────────────────────────────────────

function AttributesOnlyStep({ productId, canEdit, categoryId }: { productId: string; canEdit: boolean; categoryId?: string | null }) {
  const { t } = useTranslation("catalog");
  return (
    <div className="space-y-4">
      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("wizard.typeHint.simpleAttr")}</p>
      <ProductAttributesTab productId={productId} canEdit={canEdit} categoryId={categoryId} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────────
//  Step 5 — Technical specs + shipping/config
// ───────────────────────────────────────────────────────────────────────────

// ───────────────────────────────────────────────────────────────────────────
//  Step 3 (Bundle) — combo items
// ───────────────────────────────────────────────────────────────────────────

function BundleStep({ productId, canEdit }: { productId: string; canEdit: boolean }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const itemsQ = useQuery({ queryKey: ["catalog", "bundle-items", productId], queryFn: () => getBundleItems(productId) });
  const productsQ = useQuery({ queryKey: ["catalog", "products", "list-bundle"], queryFn: () => searchProducts({ pageSize: 100, sort: "name" }) });

  const [pickProductId, setPickProductId] = useState<string | null>(null);
  const [pickVariationId, setPickVariationId] = useState<string | null>(null);
  const [qty, setQty] = useState("1");
  const [discount, setDiscount] = useState("");

  const variationsQ = useQuery({
    queryKey: ["catalog", "variations", pickProductId],
    queryFn: () => getVariations(pickProductId!),
    enabled: !!pickProductId,
  });

  // Default to the (only / default) variation when a product is picked.
  useEffect(() => {
    const vs = (variationsQ.data ?? []).filter((v) => !v.isDeleted);
    if (vs.length > 0) setPickVariationId(vs.find((v) => v.isDefault)?.id ?? vs[0].id);
    else setPickVariationId(null);
  }, [variationsQ.data]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["catalog", "bundle-items", productId] });
  const add = useMutation({
    mutationFn: () => addBundleItem(productId, {
      itemVariationId: pickVariationId!,
      quantity: Math.max(1, Number(qty) || 1),
      discountPercent: discount ? Number(discount) : null,
    }),
    onSuccess: () => { setDiscount(""); setQty("1"); invalidate(); },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });
  const remove = useMutation({
    mutationFn: (itemId: string) => removeBundleItem(productId, itemId),
    onSuccess: invalidate,
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  const productOptions = (productsQ.data?.items ?? [])
    .filter((p) => p.id !== productId)
    .map((p) => ({ value: p.id, label: p.name }));
  const variationOptions = (variationsQ.data ?? []).filter((v) => !v.isDeleted)
    .map((v) => ({ value: v.id, label: v.sku + (v.isDefault ? " ★" : "") }));

  const items = itemsQ.data ?? [];
  const canAdd = !!pickVariationId && !add.isPending;

  // C10 — auto-generate the combo cover from the included products' images (canvas collage).
  const { upload } = useFileUpload({ ownerType: "Product", ownerId: productId, category: "Image", visibility: Visibility.Public });
  const [generating, setGenerating] = useState(false);
  const collageUrls = items.map((it) => it.itemThumbnailUrl).filter((u): u is string => !!u);
  const generateImage = async () => {
    setGenerating(true);
    try {
      const blob = await buildBundleCollage(collageUrls);
      const file = new File([blob], "combo.png", { type: "image/png" });
      const asset = await upload(file);
      const meta = await getFileMetadata(asset.id);
      if (!meta.publicUrl) throw new Error("Sin publicUrl.");
      await addProductImage(productId, { url: meta.publicUrl, isPrimary: true });
      queryClient.invalidateQueries({ queryKey: ["catalog", "product-images", productId] });
      toast.success(t("bundle.imageGenerated"));
    } catch (e) {
      toast.error(t("bundle.imageFailed"), { description: describe(e) });
    } finally {
      setGenerating(false);
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2">
        <Boxes className="size-4 text-[var(--color-muted-foreground)]" />
        <h2 className="text-sm font-semibold text-[var(--color-foreground)]">{t("bundle.title")}</h2>
        <span className="rounded-full bg-[var(--color-muted)] px-2 py-0.5 text-[11px] font-medium text-[var(--color-muted-foreground)]">{items.length}</span>
        {canEdit && collageUrls.length > 0 && (
          <Button type="button" variant="outline" size="sm" className="ml-auto" disabled={generating} onClick={generateImage}>
            <ImageIcon className="size-4" />{generating ? t("bundle.imageGenerating") : t("bundle.generateImage")}
          </Button>
        )}
      </div>
      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("bundle.intro")}</p>

      {items.length === 0 ? (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("bundle.empty")}</p>
      ) : (
        <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
          {items.map((it) => (
            <li key={it.id} className="flex items-center justify-between gap-3 px-3 py-2.5 text-[13px]">
              <div className="flex items-center gap-3">
                {it.itemThumbnailUrl
                  ? <img src={it.itemThumbnailUrl} alt="" className="size-9 shrink-0 rounded-md border border-[var(--color-border)] object-cover" />
                  : <div className="flex size-9 shrink-0 items-center justify-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)]"><ImageIcon className="size-4 text-[var(--color-muted-foreground)]" /></div>}
                <div className="flex flex-col">
                  {it.itemProductName && <span className="font-medium text-[var(--color-foreground)]">{it.itemProductName}</span>}
                  <span className="flex items-center gap-2">
                    <code className="font-mono text-[12px] text-[var(--color-muted-foreground)]">{it.itemSku}</code>
                    <span className="text-[var(--color-muted-foreground)]">× {it.quantity}</span>
                    {it.discountPercent ? <span className="text-[var(--color-success)]">−{it.discountPercent}%</span> : null}
                  </span>
                </div>
              </div>
              {canEdit && (
                <button type="button" onClick={() => remove.mutate(it.id)} aria-label={t("bundle.remove")}
                  className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canEdit && (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <FormGrid>
            <Field id="b-prod" span={4} label={t("bundle.item")}>
              <Combobox id="b-prod" label={t("bundle.item")} value={pickProductId} onChange={(v) => { setPickProductId(v); }}
                options={productOptions} searchable clearable placeholder={t("bundle.selectProduct")} />
            </Field>
            <Field id="b-var" span={4} label={t("bundle.variation")}>
              <Combobox id="b-var" label={t("bundle.variation")} value={pickVariationId} onChange={setPickVariationId}
                options={variationOptions} searchable />
            </Field>
            <Field id="b-qty" span={2} label={t("bundle.quantity")}>
              <Input id="b-qty" type="number" min={1} value={qty} onChange={(e) => setQty(e.target.value)} />
            </Field>
            <Field id="b-disc" span={2} label={t("bundle.discount")}>
              <Input id="b-disc" type="number" min={0} max={100} step="0.01" value={discount} onChange={(e) => setDiscount(e.target.value)} />
            </Field>
          </FormGrid>
          <div className="mt-3 flex justify-end">
            <Button type="button" disabled={!canAdd} onClick={() => add.mutate()}><Plus className="size-4" />{t("bundle.add")}</Button>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Friendly key-value editor for the Specs JSONB field (§2.6) ──
function specsToRows(json: string): { k: string; v: string }[] {
  try {
    const o = json.trim() ? JSON.parse(json) : {};
    if (o && typeof o === "object" && !Array.isArray(o)) {
      return Object.entries(o).map(([k, v]) => ({ k, v: typeof v === "string" ? v : JSON.stringify(v) }));
    }
  } catch { /* ignore malformed */ }
  return [];
}
function rowsToJson(rows: { k: string; v: string }[]): string {
  const o: Record<string, string> = {};
  for (const { k, v } of rows) {
    const key = k.trim();
    if (key) o[key] = v;
  }
  return Object.keys(o).length ? JSON.stringify(o) : "";
}

function SpecsEditor({ value, onChange }: { value: string; onChange: (json: string) => void }) {
  const { t } = useTranslation("catalog");
  const [rows, setRows] = useState<{ k: string; v: string }[]>(() => specsToRows(value));
  const lastEmit = useRef(value);

  // Re-sync from an external value change (e.g. product loads), but ignore our own emits.
  useEffect(() => {
    if (value !== lastEmit.current) {
      setRows(specsToRows(value));
      lastEmit.current = value;
    }
  }, [value]);

  const commit = (next: { k: string; v: string }[]) => {
    setRows(next);
    const json = rowsToJson(next);
    lastEmit.current = json;
    onChange(json);
  };

  const setRow = (i: number, patch: Partial<{ k: string; v: string }>) =>
    commit(rows.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  const removeRow = (i: number) => commit(rows.filter((_, idx) => idx !== i));
  const addRow = () => setRows((r) => [...r, { k: "", v: "" }]);

  return (
    <div className="space-y-2">
      {rows.length === 0 && (
        <p className="text-[12px] text-[var(--color-muted-foreground)]">{t("wizard.specsEmpty")}</p>
      )}
      {rows.map((row, i) => (
        <div key={i} className="flex items-center gap-2">
          <Input
            value={row.k}
            onChange={(e) => setRow(i, { k: e.target.value })}
            placeholder={t("wizard.specsKeyPlaceholder")}
            className="w-1/3 font-mono text-[12.5px]"
            aria-label={t("wizard.specsKey")}
          />
          <span aria-hidden className="text-[var(--color-muted-foreground)]">→</span>
          <Input
            value={row.v}
            onChange={(e) => setRow(i, { v: e.target.value })}
            placeholder={t("wizard.specsValuePlaceholder")}
            className="grow text-[12.5px]"
            aria-label={t("wizard.specsValue")}
            onKeyDown={(e) => {
              if (e.key === "Enter") { e.preventDefault(); if (i === rows.length - 1) addRow(); }
            }}
          />
          <button
            type="button"
            onClick={() => removeRow(i)}
            aria-label={t("wizard.specsKey")}
            className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-destructive)]"
          >
            <Trash2 className="size-4" />
          </button>
        </div>
      ))}
      <Button type="button" variant="outline" size="sm" onClick={addRow}>
        <Plus className="size-4" />
        {t("wizard.specsAddRow")}
      </Button>
    </div>
  );
}

function SpecsStep(props: {
  technicalSpecs: string; setTechnicalSpecs: (v: string) => void;
  specs: string; setSpecs: (v: string) => void; specsValid: boolean;
  taxRateId: string | null; setTaxRateId: (v: string | null) => void;
  shippingClassId: string | null; setShippingClassId: (v: string | null) => void;
  weight: string; setWeight: (v: string) => void; weightUnit: string; setWeightUnit: (v: string) => void;
  dimL: string; setDimL: (v: string) => void; dimW: string; setDimW: (v: string) => void;
  dimH: string; setDimH: (v: string) => void; dimUnit: string; setDimUnit: (v: string) => void;
}) {
  const { t } = useTranslation("catalog");
  const taxQuery = useQuery({ queryKey: ["catalog", "tax-rates"], queryFn: () => getTaxRates(true) });
  const shipQuery = useQuery({ queryKey: ["catalog", "shipping-classes"], queryFn: () => getShippingClasses() });

  const taxOptions = (taxQuery.data ?? []).map((x) => ({ value: x.id, label: `${x.name} (${Math.round(x.rate * 100)}%)` }));
  const shipOptions = (shipQuery.data ?? []).map((x) => ({ value: x.id, label: x.name }));

  return (
    <FormGrid>
      <Field id="p-tax" span={6} label={t("products.fields.taxRate", "Impuesto")}>
        <Combobox id="p-tax" label={t("products.fields.taxRate", "Impuesto")} placeholder={t("products.fields.taxRate", "Impuesto")}
          value={props.taxRateId} onChange={props.setTaxRateId} options={taxOptions} clearable searchable />
      </Field>
      <Field id="p-ship" span={6} label={t("products.fields.shippingClass", "Clase de envío")}>
        <Combobox id="p-ship" label={t("products.fields.shippingClass", "Clase de envío")} placeholder={t("products.fields.shippingClass", "Clase de envío")}
          value={props.shippingClassId} onChange={props.setShippingClassId} options={shipOptions} clearable searchable />
      </Field>

      <Field id="p-weight" span={3} label={t("inventory.weight", "Peso")}>
        <Input id="p-weight" type="number" min={0} step="0.001" value={props.weight} onChange={(e) => props.setWeight(e.target.value)} />
      </Field>
      <Field id="p-wunit" span={3} label={t("inventory.weightUnit", "Unidad")}>
        <Combobox id="p-wunit" label="wu" value={props.weightUnit} onChange={(v) => v && props.setWeightUnit(v)} options={WEIGHT_UNITS.map((u) => ({ value: u, label: u }))} />
      </Field>
      <Field id="p-dimL" span={2} label="L">
        <Input id="p-dimL" type="number" min={0} step="0.1" value={props.dimL} onChange={(e) => props.setDimL(e.target.value)} />
      </Field>
      <Field id="p-dimW" span={2} label="W">
        <Input id="p-dimW" type="number" min={0} step="0.1" value={props.dimW} onChange={(e) => props.setDimW(e.target.value)} />
      </Field>
      <Field id="p-dimH" span={2} label="H">
        <Input id="p-dimH" type="number" min={0} step="0.1" value={props.dimH} onChange={(e) => props.setDimH(e.target.value)} />
      </Field>

      <Field id="p-tech" span={12} label={t("detail.tabs.specs", "Especificaciones técnicas")}>
        <MakaRichTextEditor id="p-tech" value={props.technicalSpecs} onChange={props.setTechnicalSpecs}
          height={220} maxLength={10000} placeholder={t("detail.tabs.specs", "")} />
      </Field>
      <Field id="p-specs" span={12} label={t("wizard.specsJsonHint")}>
        <SpecsEditor value={props.specs} onChange={props.setSpecs} />
      </Field>
    </FormGrid>
  );
}
