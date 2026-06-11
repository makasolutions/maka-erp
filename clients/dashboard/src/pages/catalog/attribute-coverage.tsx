import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, ClipboardCheck, SlidersHorizontal, X } from "lucide-react";
import { toast } from "sonner";
import {
  getCategoryCoverageReport, getCategoryRequirements, getCategoryTree, searchAttributes,
  setCategoryRequirements, MARKETPLACES,
  type CategoryDto, type Marketplace,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, EntityPageHeader } from "@/components/list";
import { MakaGridClient, MakaGridFilters, MakaFilterField } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type FlatCategory = { id: string; label: string };
function flatten(nodes: CategoryDto[], depth = 0, acc: FlatCategory[] = []): FlatCategory[] {
  for (const n of nodes) {
    acc.push({ id: n.id, label: `${"— ".repeat(depth)}${n.name}` });
    if (n.children?.length) flatten(n.children, depth + 1, acc);
  }
  return acc;
}

type CoverageRow = { productId: string; productName: string; coveredLabel: string; missingLabel: string };

function CoveredCell(row: CoverageRow) {
  return <span className="tabular-nums text-[13px] text-[var(--color-foreground)]">{row.coveredLabel}</span>;
}
function MissingCell(row: CoverageRow) {
  if (!row.missingLabel) return <span className="text-[12px] text-[var(--color-success)]">✓</span>;
  return <span className="text-[12px] text-[var(--color-warning)]" title={row.missingLabel}>{row.missingLabel}</span>;
}

export function AttributeCoveragePage() {
  const { t } = useTranslation("catalog");
  const { can } = usePerm();

  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [marketplace, setMarketplace] = useState<Marketplace | "template">("template");
  const [reqOpen, setReqOpen] = useState(false);

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: () => getCategoryTree(),
    placeholderData: keepPreviousData,
  });
  const catOptions = useMemo(
    () => flatten(categoriesQuery.data ?? []).map((c) => ({ value: c.id, label: c.label })),
    [categoriesQuery.data],
  );

  const mkParam = marketplace === "template" ? undefined : marketplace;
  const reportQuery = useQuery({
    queryKey: ["catalog", "coverage", categoryId, mkParam ?? "template"],
    queryFn: () => getCategoryCoverageReport(categoryId!, mkParam),
    enabled: !!categoryId,
  });
  const report = reportQuery.data;

  const rows: CoverageRow[] = useMemo(() => {
    if (!report) return [];
    const nameById = new Map(report.attributes.map((a) => [a.id, a.name]));
    return report.products.map((p) => {
      const covered = new Set(p.coveredAttributeIds);
      const missing = report.attributes.filter((a) => !covered.has(a.id)).map((a) => a.name);
      return {
        productId: p.productId,
        productName: p.productName,
        coveredLabel: `${p.coveredAttributeIds.filter((id) => nameById.has(id)).length} / ${report.attributes.length}`,
        missingLabel: missing.join(", "),
      };
    });
  }, [report]);

  const columns: ColumnModel[] = useMemo(() => [
    { field: "productName", headerText: t("coverage.product"), minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "coveredLabel", headerText: t("coverage.coveredCount"), template: CoveredCell as any, width: 120, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "missingLabel", headerText: t("coverage.missing"), template: MissingCell as any, minWidth: 240, allowSorting: false },
  ], [t]);

  const mkOptions = [
    { value: "template", label: t("coverage.template") },
    ...MARKETPLACES.map((m) => ({ value: m, label: t(`marketplace.${m}`) })),
  ];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ClipboardCheck}
        title={t("coverage.title")}
        total={report?.products.length ?? null}
        unit={t("coverage.product")}
        description={t("coverage.description")}
      >
        {can(P.catalog.attributes.manage) && categoryId && mkParam && (
          <Button onClick={() => setReqOpen(true)}
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
            <SlidersHorizontal className="size-4" />{t("coverage.configureRequirements")}
          </Button>
        )}
      </EntityPageHeader>

      <MakaGridFilters
        open
        filters={
          <>
            <MakaFilterField label={t("coverage.category")} className="grow">
              <Combobox id="cov-cat" label={t("coverage.category")} value={categoryId} onChange={setCategoryId}
                options={catOptions} searchable clearable placeholder={t("coverage.category")} />
            </MakaFilterField>
            <MakaFilterField label={t("coverage.marketplace")}>
              <Combobox id="cov-mk" label={t("coverage.marketplace")} value={marketplace}
                onChange={(v) => setMarketplace((v as Marketplace | "template") ?? "template")} options={mkOptions} />
            </MakaFilterField>
          </>
        }
      />

      {!categoryId ? (
        <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
          {t("coverage.selectCategory")}
        </p>
      ) : (
        <>
          {/* Per-attribute summary */}
          <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
            {(report?.summary ?? []).length === 0 ? (
              <p className="text-[13px] text-[var(--color-muted-foreground)]">{t("coverage.noAttributes")}</p>
            ) : (
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {(report?.summary ?? []).map((s) => {
                  const pct = s.total > 0 ? Math.round((s.covered / s.total) * 100) : 0;
                  return (
                    <div key={s.attributeId} className="rounded-md border border-[var(--color-border)] bg-[var(--color-background)] p-3">
                      <div className="flex items-center justify-between gap-2">
                        <span className="truncate text-[13px] font-medium text-[var(--color-foreground)]" title={s.name}>{s.name}</span>
                        <span className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
                          {t("coverage.coveredOf", { covered: s.covered, total: s.total })}
                        </span>
                      </div>
                      <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-[var(--color-muted)]">
                        <div className="h-full rounded-full bg-[var(--color-accent)]" style={{ width: `${pct}%` }} />
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <MakaGridClient<CoverageRow>
            dataSource={rows}
            columns={columns}
            isLoading={reportQuery.isFetching}
            fileName="cobertura-atributos"
            entityName={t("coverage.product")}
          />
        </>
      )}

      {categoryId && mkParam && (
        <RequirementsDialog
          open={reqOpen}
          onClose={() => setReqOpen(false)}
          categoryId={categoryId}
          marketplace={mkParam}
        />
      )}
    </div>
  );
}

function RequirementsDialog({ open, onClose, categoryId, marketplace }: {
  open: boolean; onClose: () => void; categoryId: string; marketplace: Marketplace;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const [showAll, setShowAll] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);

  const attrsQuery = useQuery({
    queryKey: ["catalog", "attributes", "list", showAll ? "all" : categoryId],
    queryFn: () => searchAttributes({ pageSize: 200, sort: "name", categoryId: showAll ? undefined : categoryId }),
    enabled: open,
  });
  const currentReq = useQuery({
    queryKey: ["catalog", "requirements", categoryId],
    queryFn: () => getCategoryRequirements(categoryId),
    enabled: open,
  });

  useEffect(() => {
    if (open && currentReq.data) {
      const g = currentReq.data.groups.find((x) => x.marketplace === marketplace);
      setSelected(g?.attributeIds ?? []);
    }
  }, [open, currentReq.data, marketplace]);

  const attributes = attrsQuery.data?.items ?? [];
  const toggle = (id: string, on: boolean) =>
    setSelected((prev) => (on ? [...new Set([...prev, id])] : prev.filter((x) => x !== id)));

  const saveM = useMutation({
    mutationFn: () => setCategoryRequirements(categoryId, marketplace, selected),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "requirements", categoryId] });
      queryClient.invalidateQueries({ queryKey: ["catalog", "coverage"] });
      queryClient.invalidateQueries({ queryKey: ["catalog", "marketplace-validation"] });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{t("coverage.requirementsTitle", { marketplace: t(`marketplace.${marketplace}`) })}</DialogTitle>
          <DialogDescription>{t("coverage.requirementsDesc", { marketplace: t(`marketplace.${marketplace}`) })}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="mb-3 flex justify-end">
            <label className="flex items-center gap-2 text-[12.5px] text-[var(--color-foreground)]">
              <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)}
                className="size-3.5 accent-[var(--color-primary)]" />
              {t("coverage.showAll")}
            </label>
          </div>
          {attributes.length === 0 ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">{t("attributes.noCategories")}</p>
          ) : (
            <div className="grid grid-cols-2 gap-x-4 gap-y-1.5 sm:grid-cols-3">
              {attributes.map((a) => (
                <label key={a.id} className="flex items-center gap-2 text-[12.5px] text-[var(--color-foreground)]">
                  <input type="checkbox" checked={selected.includes(a.id)} onChange={(e) => toggle(a.id, e.target.checked)}
                    className="size-3.5 accent-[var(--color-primary)]" />
                  <span className="truncate" title={a.name}>{a.name}</span>
                </label>
              ))}
            </div>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={saveM.isPending}><X className="size-4" />{tc("actions.cancel")}</Button>
          </DialogClose>
          <Button type="button" onClick={() => saveM.mutate()} disabled={saveM.isPending}>
            <Check className="size-4" />{saveM.isPending ? tc("feedback.saving") : t("coverage.save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
