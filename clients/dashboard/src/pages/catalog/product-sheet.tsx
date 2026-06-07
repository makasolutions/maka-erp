import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, FileDown, ImageOff } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
  getBundleItems, getDefaultVariation, getEffectivePrice, getProductById, getProductCodes,
  getProductImages, getVariations,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import { Combobox, EntityStatusBadge } from "@/components/list";
import { formatMoney } from "@/lib/list-helpers";
import { ShareMenu } from "@/components/catalog/share-menu";
import { tokenStore } from "@/auth/token-store";
import { cn } from "@/lib/cn";

const PRINT_CSS = `
@media print {
  body * { visibility: hidden !important; }
  #product-sheet, #product-sheet * { visibility: visible !important; }
  #product-sheet { position: absolute; inset: 0; margin: 0; padding: 16px; }
  .no-print { display: none !important; }
}`;

export function ProductSheetPage() {
  const { productId = "" } = useParams();
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");

  const productQ = useQuery({ queryKey: ["catalog", "products", "detail", productId], queryFn: () => getProductById(productId), enabled: !!productId });
  const imagesQ = useQuery({ queryKey: ["catalog", "product-images", productId], queryFn: () => getProductImages(productId), enabled: !!productId });
  const variationsQ = useQuery({ queryKey: ["catalog", "variations", productId], queryFn: () => getVariations(productId), enabled: !!productId });
  const defVarQ = useQuery({ queryKey: ["catalog", "default-variation", productId], queryFn: () => getDefaultVariation(productId), enabled: !!productId });
  const defVarId = defVarQ.data?.id;
  const codesQ = useQuery({ queryKey: ["catalog", "codes", productId, defVarId], queryFn: () => getProductCodes(productId, defVarId!), enabled: !!defVarId });

  const product = productQ.data;
  const isBundle = product?.type === "Bundle";
  const bundleQ = useQuery({ queryKey: ["catalog", "bundle-items", productId], queryFn: () => getBundleItems(productId), enabled: !!productId && isBundle });

  const images = useMemo(() => [...(imagesQ.data ?? [])].sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary) || a.sortOrder - b.sortOrder), [imagesQ.data]);
  const [selected, setSelected] = useState(0);
  const main = images[selected] ?? images[0];

  const variations = (variationsQ.data ?? []).filter((v) => !v.isDeleted);
  const specsEntries = useMemo(() => {
    if (!product?.specs) return [];
    try { const o = JSON.parse(product.specs); return o && typeof o === "object" ? Object.entries(o) : []; } catch { return []; }
  }, [product?.specs]);

  const statusTone = product?.status === "Active" ? "success" : product?.status === "Draft" ? "default" : "warning";

  return (
    <div className="space-y-4 sm:space-y-6">
      <style>{PRINT_CSS}</style>

      <div className="no-print flex items-center justify-between gap-2">
        <Link to={`/catalog/products/${productId}`} className="inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]">
          <ArrowLeft className="size-4" />{t("sheet.back")}
        </Link>
        <div className="flex items-center gap-2">
          {product?.slug && (
            <ShareMenu
              title={product.name}
              url={`${window.location.origin}/p/${tokenStore.getTenant() ?? "root"}/${product.slug}`}
            />
          )}
          <Button onClick={() => window.print()} className="h-9 gap-1.5">
            <FileDown className="size-4" />{t("sheet.exportPdf")}
          </Button>
        </div>
      </div>

      <div id="product-sheet" className="space-y-6 rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-5 sm:p-8">
        {/* Header */}
        <div className="flex flex-wrap items-start justify-between gap-3 border-b border-[var(--color-border)] pb-4">
          <div>
            <h1 className="font-display text-[26px] font-semibold leading-tight text-[var(--color-foreground)]">{product?.name ?? "…"}</h1>
            <div className="mt-1 flex flex-wrap items-center gap-2 text-[13px] text-[var(--color-muted-foreground)]">
              {product && <EntityStatusBadge tone={statusTone}>{t(`products.statuses.${product.status}`, product.status)}</EntityStatusBadge>}
              {product?.brandName && <span>· {product.brandName}</span>}
              {product?.type && <span>· {t(`products.types.${product.type}`, product.type)}</span>}
              {product?.slug && <code className="font-mono text-[11px]">/{product.slug}</code>}
            </div>
          </div>
        </div>

        {/* Gallery + key info */}
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="space-y-3">
            <div className="grid aspect-square w-full place-items-center overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)]">
              {main ? (
                <img src={main.url} alt={main.altText ?? ""} className="h-full w-full object-contain" />
              ) : (
                <div className="flex flex-col items-center gap-2 text-[var(--color-muted-foreground)]">
                  <ImageOff className="size-8" /><span className="text-[12px]">{t("sheet.noImages")}</span>
                </div>
              )}
            </div>
            {images.length > 1 && (
              <div className="flex flex-wrap gap-2">
                {images.map((img, i) => (
                  <button key={img.id} type="button" onClick={() => setSelected(i)}
                    className={cn("size-14 overflow-hidden rounded-lg border", i === selected ? "border-[var(--color-primary)] ring-1 ring-[var(--color-primary)]" : "border-[var(--color-border)]")}>
                    <img src={img.url} alt="" className="h-full w-full object-cover" />
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="space-y-5">
            {product?.shortDescription && (
              <section>
                <h2 className="mb-1 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">{t("sheet.shortDescription")}</h2>
                <div className="prose-sm text-[14px] text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: product.shortDescription }} />
              </section>
            )}
            <section>
              <h2 className="mb-2 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">{t("sheet.codes")}</h2>
              <ul className="flex flex-wrap gap-2">
                {defVarQ.data?.sku && <CodeChip type="SKU" code={defVarQ.data.sku} />}
                {(codesQ.data ?? []).filter((c) => c.codeType !== "SKU").map((c) => <CodeChip key={c.id} type={c.codeType} code={c.code} />)}
                {!defVarQ.data?.sku && (codesQ.data ?? []).length === 0 && <span className="text-[12px] text-[var(--color-muted-foreground)]">—</span>}
              </ul>
            </section>
            {defVarId && <EffectivePriceSection variationId={defVarId} />}
          </div>
        </div>

        {/* Long description */}
        {product?.description && (
          <section className="border-t border-[var(--color-border)] pt-5">
            <h2 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.description")}</h2>
            <div className="text-[14px] leading-relaxed text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: product.description }} />
          </section>
        )}

        {/* Variations */}
        {variations.length > 0 && (
          <section className="border-t border-[var(--color-border)] pt-5">
            <h2 className="mb-3 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.variations")} ({variations.length})</h2>
            <div className="overflow-x-auto">
              <table className="w-full text-[13px]">
                <thead><tr className="border-b border-[var(--color-border)] text-left text-[var(--color-muted-foreground)]">
                  <th className="py-1.5 pr-4 font-medium">SKU</th><th className="py-1.5 pr-4 font-medium">{t("variations.combination")}</th>
                </tr></thead>
                <tbody>
                  {variations.map((v) => (
                    <tr key={v.id} className="border-b border-[var(--color-border)]">
                      <td className="py-1.5 pr-4"><code className="font-mono">{v.sku}</code>{v.isDefault && " ★"}</td>
                      <td className="py-1.5 pr-4">{v.attributeValues?.map((av) => `${av.attributeName}: ${av.value}`).join(", ") || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* Bundle items */}
        {isBundle && (bundleQ.data ?? []).length > 0 && (
          <section className="border-t border-[var(--color-border)] pt-5">
            <h2 className="mb-3 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.bundle")}</h2>
            <ul className="space-y-1 text-[13px]">
              {(bundleQ.data ?? []).map((b) => (
                <li key={b.id}><code className="font-mono">{b.itemSku}</code> × {b.quantity}{b.discountPercent ? ` (−${b.discountPercent}%)` : ""}</li>
              ))}
            </ul>
          </section>
        )}

        {/* Specs */}
        {specsEntries.length > 0 && (
          <section className="border-t border-[var(--color-border)] pt-5">
            <h2 className="mb-3 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.specs")}</h2>
            <table className="w-full max-w-xl text-[13px]">
              <tbody>
                {specsEntries.map(([k, v]) => (
                  <tr key={k} className="border-b border-[var(--color-border)]">
                    <td className="py-1.5 pr-4 font-medium capitalize text-[var(--color-muted-foreground)]">{k}</td>
                    <td className="py-1.5 text-[var(--color-foreground)]">{String(v)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )}

        {/* Technical specs (HTML) */}
        {product?.technicalSpecs && (
          <section className="border-t border-[var(--color-border)] pt-5">
            <div className="text-[14px] leading-relaxed text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: product.technicalSpecs }} />
          </section>
        )}

        <p className="border-t border-[var(--color-border)] pt-3 text-[11px] text-[var(--color-muted-foreground)]">
          {tc("appName", "Maka")} · {t("sheet.title")}
        </p>
      </div>
    </div>
  );
}

const PRICE_SEGMENTS = ["Retail", "B2B", "VIP", "Mayorista"];

function EffectivePriceSection({ variationId }: { variationId: string }) {
  const { t } = useTranslation("catalog");
  const [segment, setSegment] = useState("Retail");
  const priceQ = useQuery({
    queryKey: ["catalog", "effective-price", variationId, segment],
    queryFn: () => getEffectivePrice(variationId, segment),
    enabled: !!variationId,
    retry: false,
  });
  const p = priceQ.data;

  return (
    <section className="no-print">
      <div className="mb-1 flex items-center justify-between gap-2">
        <h2 className="text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">{t("priceLists.effectivePrice")}</h2>
        <div className="w-32">
          <Combobox id="ps-segment" label={t("priceLists.fields.segment")} value={segment}
            onChange={(v) => v && setSegment(v)} options={PRICE_SEGMENTS.map((s) => ({ value: s, label: s }))} />
        </div>
      </div>
      {priceQ.isError || !p ? (
        <span className="text-[13px] text-[var(--color-muted-foreground)]">—</span>
      ) : (
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="font-display text-[22px] font-semibold tabular-nums text-[var(--color-foreground)]">{formatMoney(p.effectivePrice)}</span>
          {p.isCampaign && (
            <EntityStatusBadge tone="warning">{t("campaigns.inCampaign")}</EntityStatusBadge>
          )}
          {p.isSalePrice && !p.isCampaign && (
            <>
              <span className="text-[13px] text-[var(--color-muted-foreground)] line-through tabular-nums">{formatMoney(p.listPrice)}</span>
              <EntityStatusBadge tone="success">{t("priceLists.onSale")}</EntityStatusBadge>
            </>
          )}
          {p.priceListName && (
            <span className="text-[12px] text-[var(--color-muted-foreground)]">· {p.priceListName}</span>
          )}
        </div>
      )}
    </section>
  );
}

function CodeChip({ type, code }: { type: string; code: string }) {
  return (
    <li className="flex items-center gap-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2 py-1 text-[12px]">
      <span className="font-semibold text-[var(--color-muted-foreground)]">{type}</span>
      <code className="font-mono text-[var(--color-foreground)]">{code}</code>
    </li>
  );
}
