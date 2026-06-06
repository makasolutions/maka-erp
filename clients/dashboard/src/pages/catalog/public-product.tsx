import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { FileDown, ImageOff, PackageX } from "lucide-react";
import { useTranslation } from "react-i18next";
import { getPublicProduct } from "@/api/catalog";
import { Button } from "@/components/ui/button";
import { ShareMenu } from "@/components/catalog/share-menu";
import { cn } from "@/lib/cn";

const PRINT_CSS = `@media print { .no-print { display: none !important; } }`;

export function PublicProductPage() {
  const { tenant = "", slug = "" } = useParams();
  const { t } = useTranslation("catalog");

  const q = useQuery({
    queryKey: ["public-product", tenant, slug],
    queryFn: () => getPublicProduct(tenant, slug),
    retry: false,
  });

  const p = q.data;
  const images = useMemo(
    () => [...(p?.images ?? [])].sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary) || a.sortOrder - b.sortOrder),
    [p?.images],
  );
  const [selected, setSelected] = useState(0);
  const main = images[selected] ?? images[0];

  const specsEntries = useMemo(() => {
    if (!p?.specs) return [] as [string, unknown][];
    try { const o = JSON.parse(p.specs); return o && typeof o === "object" ? Object.entries(o) : []; } catch { return []; }
  }, [p?.specs]);

  if (q.isError) {
    return (
      <div className="grid min-h-screen place-items-center bg-[var(--color-background)] px-4">
        <div className="flex flex-col items-center gap-3 text-center text-[var(--color-muted-foreground)]">
          <PackageX className="size-10" />
          <p className="text-sm">{t("sheet.publicHint")}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[var(--color-background)] py-6 sm:py-10">
      <style>{PRINT_CSS}</style>
      <div className="mx-auto w-full max-w-4xl px-4 sm:px-6">
        <div className="no-print mb-4 flex items-center justify-between gap-2">
          <span className="font-display text-[15px] font-semibold text-[var(--color-foreground)]">Maka</span>
          <div className="flex items-center gap-2">
            {p && <ShareMenu title={p.name} url={window.location.href} />}
            <Button variant="outline" onClick={() => window.print()} className="h-9 gap-1.5">
              <FileDown className="size-4" />{t("sheet.exportPdf")}
            </Button>
          </div>
        </div>

        <div className="space-y-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-5 sm:p-8">
          <div className="border-b border-[var(--color-border)] pb-4">
            <h1 className="font-display text-[26px] font-semibold leading-tight text-[var(--color-foreground)]">{p?.name ?? "…"}</h1>
            <div className="mt-1 flex flex-wrap items-center gap-2 text-[13px] text-[var(--color-muted-foreground)]">
              {p?.brandName && <span>{p.brandName}</span>}
              {p?.type && <span>· {t(`products.types.${p.type}`, p.type)}</span>}
            </div>
          </div>

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
                    <button key={i} type="button" onClick={() => setSelected(i)}
                      className={cn("size-14 overflow-hidden rounded-lg border", i === selected ? "border-[var(--color-primary)] ring-1 ring-[var(--color-primary)]" : "border-[var(--color-border)]")}>
                      <img src={img.url} alt="" className="h-full w-full object-cover" />
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="space-y-5">
              {p?.shortDescription && (
                <section>
                  <h2 className="mb-1 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">{t("sheet.shortDescription")}</h2>
                  <div className="text-[14px] text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: p.shortDescription }} />
                </section>
              )}
              {(p?.codes ?? []).length > 0 && (
                <section>
                  <h2 className="mb-2 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">{t("sheet.codes")}</h2>
                  <ul className="flex flex-wrap gap-2">
                    {p!.codes.map((c) => (
                      <li key={`${c.codeType}-${c.code}`} className="flex items-center gap-1.5 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2 py-1 text-[12px]">
                        <span className="font-semibold text-[var(--color-muted-foreground)]">{c.codeType}</span>
                        <code className="font-mono text-[var(--color-foreground)]">{c.code}</code>
                      </li>
                    ))}
                  </ul>
                </section>
              )}
            </div>
          </div>

          {p?.description && (
            <section className="border-t border-[var(--color-border)] pt-5">
              <h2 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.description")}</h2>
              <div className="text-[14px] leading-relaxed text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: p.description }} />
            </section>
          )}

          {(p?.variations ?? []).length > 0 && (
            <section className="border-t border-[var(--color-border)] pt-5">
              <h2 className="mb-3 text-sm font-semibold text-[var(--color-foreground)]">{t("sheet.variations")}</h2>
              <table className="w-full text-[13px]">
                <tbody>
                  {p!.variations.map((v) => (
                    <tr key={v.sku} className="border-b border-[var(--color-border)]">
                      <td className="py-1.5 pr-4"><code className="font-mono">{v.sku}</code></td>
                      <td className="py-1.5 text-[var(--color-muted-foreground)]">{v.combination || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

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

          {p?.technicalSpecs && (
            <section className="border-t border-[var(--color-border)] pt-5">
              <div className="text-[14px] leading-relaxed text-[var(--color-foreground)]" dangerouslySetInnerHTML={{ __html: p.technicalSpecs }} />
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
