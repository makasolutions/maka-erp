import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Gauge, Plus } from "lucide-react";
import { toast } from "sonner";
import {
  closeScorecard, computeWeightedScore, createScorecard, deleteScorecard, deleteScorecardKpi,
  getScorecardById, getScorecardKpis, getScorecards, getSupplierRanking, getSupplierScoreTrend,
  gradeFor, seedDefaultKpis, updateScorecard, upsertScorecardKpi,
  type ScorecardCriterionInput, type ScorecardDto, type ScorecardGrade, type ScorecardKpiDto,
  type ScorecardStatus, type ScorecardWriteInput,
} from "@/api/scorecards";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, EntityPageHeader, EntityStatusBadge, Field, FormActions, FormErrorSummary, FormGrid } from "@/components/list";
import { SaveIcon, CancelIcon } from "@/components/ui/icons";
import { rules, validateSchema } from "@/lib/validation/rules";
import { MakaGridClient, MakaChart } from "@/components/maka";
import { PartyPicker } from "@/components/party/PartyPicker";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

type TabId = "scorecards" | "reports" | "kpis";

function gradeTone(g: ScorecardGrade): "success" | "warning" | "danger" {
  return g === "A" || g === "B" ? "success" : g === "C" ? "warning" : "danger";
}
function statusTone(s: ScorecardStatus): "success" | "default" {
  return s === "Cerrado" ? "success" : "default";
}

export function EvaluacionProveedoresPage() {
  const { t } = useTranslation("crm");
  const [tab, setTab] = useState<TabId>("scorecards");

  const tabs: { id: TabId; label: string }[] = [
    { id: "scorecards", label: t("scorecards.tabs.scorecards") },
    { id: "reports", label: t("scorecards.tabs.reports") },
    { id: "kpis", label: t("scorecards.tabs.kpis") },
  ];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader icon={Gauge} title={t("scorecards.title")} description={t("scorecards.description")} />
      <div className="flex flex-wrap gap-1 border-b border-[var(--color-border)]">
        {tabs.map((tb) => (
          <button key={tb.id} type="button" onClick={() => setTab(tb.id)}
            className={`-mb-px rounded-t-lg border-b-2 px-3 py-2 text-[13px] font-semibold transition-colors ${
              tab === tb.id ? "border-[var(--color-primary)] text-[var(--color-foreground)]"
                : "border-transparent text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"}`}>
            {tb.label}
          </button>
        ))}
      </div>
      {tab === "scorecards" && <ScorecardsTab />}
      {tab === "reports" && <ReportsTab />}
      {tab === "kpis" && <KpisTab />}
    </div>
  );
}

// ───────────────────────── Scorecards tab ─────────────────────────
type Editor = { mode: "closed" } | { mode: "create" } | { mode: "edit"; id: string } | { mode: "delete"; row: ScorecardDto };

function ScorecardsTab() {
  const { t } = useTranslation("crm");
  const { can } = usePerm();
  const [editor, setEditor] = useState<Editor>({ mode: "closed" });

  const listQ = useQuery({
    queryKey: ["crm", "scorecards", "list"],
    queryFn: () => getScorecards({ pageSize: 200, sort: "-period" }),
    placeholderData: keepPreviousData,
  });

  const rows = useMemo(() => (listQ.data?.items ?? []).map((s) => ({
    ...s,
    supplierLabel: s.supplierName ?? "—",
    statusLabel: t(`scorecards.status.${s.status}`),
  })), [listQ.data, t]);

  type Row = (typeof rows)[number];
  const columns: ColumnModel[] = useMemo(() => [
    { field: "supplierLabel", headerText: t("scorecards.fields.supplier"), minWidth: 200 },
    { field: "periodLabel", headerText: t("scorecards.fields.period"), width: 130 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "weightedScore", headerText: t("scorecards.fields.score"), width: 110, textAlign: "Center",
      template: ((r: Row) => <span className="font-semibold tabular-nums">{r.weightedScore.toFixed(2)}</span>) as any },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "grade", headerText: t("scorecards.fields.grade"), width: 90, textAlign: "Center",
      template: ((r: Row) => <EntityStatusBadge tone={gradeTone(r.grade)}>{r.grade}</EntityStatusBadge>) as any },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "statusLabel", headerText: t("scorecards.fields.status"), width: 120, textAlign: "Center",
      template: ((r: Row) => <EntityStatusBadge tone={statusTone(r.status)}>{r.statusLabel}</EntityStatusBadge>) as any },
  ], [t]);

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button perm={P.catalog.scorecards.manage} onClick={() => setEditor({ mode: "create" })}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
          <Plus className="size-4" />{t("scorecards.actions.create")}
        </Button>
      </div>
      <MakaGridClient<Row>
        dataSource={rows} columns={columns} isLoading={listQ.isFetching}
        fileName="scorecards" entityName={t("scorecards.singular")}
        onRowClick={(r) => can(P.catalog.scorecards.view) && setEditor({ mode: "edit", id: r.id })}
        permissions={{ edit: P.catalog.scorecards.view, delete: P.catalog.scorecards.manage }}
        onEdit={(r) => setEditor({ mode: "edit", id: r.id })}
        onDelete={(r) => setEditor({ mode: "delete", row: r })}
      />
      <ScorecardEditor state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteScorecardDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

type FormState = {
  supplierId: string | null;
  periodLabel: string;
  periodStart: string;
  notes: string;
  criteria: ScorecardCriterionInput[];
};

function ScorecardEditor({ state, onClose }: { state: Editor; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isCreate = state.mode === "create";
  const editId = state.mode === "edit" ? state.id : undefined;
  const isOpen = isCreate || state.mode === "edit";

  const [form, setForm] = useState<FormState>({ supplierId: null, periodLabel: "", periodStart: new Date().toISOString().slice(0, 10), notes: "", criteria: [] });
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const set = (patch: Partial<FormState>) => setForm((f) => ({ ...f, ...patch }));

  const detailQ = useQuery({ queryKey: ["crm", "scorecards", "detail", editId], queryFn: () => getScorecardById(editId!), enabled: !!editId });
  const kpisQ = useQuery({ queryKey: ["crm", "scorecard-kpis", "active"], queryFn: () => getScorecardKpis(true), enabled: isOpen && isCreate });
  const detail = detailQ.data;
  const readOnly = !!detail && !detail.isMutable;

  useEffect(() => {
    if (!isOpen) return;
    setErrorMsg(null);
    if (isCreate) {
      const criteria = (kpisQ.data ?? []).map((k) => ({ kpiCode: k.code, kpiName: k.name, weight: k.weight, score: 3, comment: null }));
      setForm({ supplierId: null, periodLabel: "", periodStart: new Date().toISOString().slice(0, 10), notes: "", criteria });
    } else if (detail) {
      setForm({
        supplierId: detail.supplierId, periodLabel: detail.periodLabel, periodStart: detail.periodStart.slice(0, 10),
        notes: detail.notes ?? "",
        criteria: detail.criteria.map((c) => ({ kpiCode: c.kpiCode, kpiName: c.kpiName, weight: c.weight, score: c.score, comment: c.comment })),
      });
    }
  }, [isOpen, isCreate, detail, kpisQ.data]);

  const liveScore = useMemo(() => computeWeightedScore(form.criteria), [form.criteria]);
  const liveGrade = gradeFor(liveScore);

  const save = useMutation({
    mutationFn: async () => {
      const input: ScorecardWriteInput = {
        supplierId: form.supplierId ?? "", periodLabel: form.periodLabel.trim(), periodStart: form.periodStart,
        notes: form.notes.trim() || null, criteria: form.criteria,
      };
      if (isCreate) await createScorecard(input); else await updateScorecard(editId!, input);
    },
    onSuccess: () => { toast.success(isCreate ? tc("feedback.created") : tc("feedback.updated")); queryClient.invalidateQueries({ queryKey: ["crm", "scorecards"] }); onClose(); },
    onError: (e) => { setErrorMsg(describe(e)); toast.error(tc("feedback.saveFailed"), { description: describe(e) }); },
  });

  const closeMut = useMutation({
    mutationFn: () => closeScorecard(editId!),
    onSuccess: () => { toast.success(tc("feedback.updated")); queryClient.invalidateQueries({ queryKey: ["crm", "scorecards"] }); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const updateCriterion = (i: number, patch: Partial<ScorecardCriterionInput>) =>
    set({ criteria: form.criteria.map((c, idx) => (idx === i ? { ...c, ...patch } : c)) });

  const [showErrors, setShowErrors] = useState(false);
  const fieldErrs = showErrors
    ? validateSchema(
        { supplierId: form.supplierId ?? "", periodLabel: form.periodLabel, notes: form.notes },
        { supplierId: [rules.required()], periodLabel: [rules.required(), rules.max(32)], notes: [rules.max(2000)] },
        tc,
      )
    : ({} as Record<string, string>);

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const v = validateSchema(
      { supplierId: form.supplierId ?? "", periodLabel: form.periodLabel, notes: form.notes },
      { supplierId: [rules.required()], periodLabel: [rules.required(), rules.max(32)], notes: [rules.max(2000)] },
      tc,
    );
    const badCriteria = form.criteria.some((c) => c.score < 0 || c.score > 5);
    if (Object.keys(v).length > 0 || badCriteria) {
      setShowErrors(true);
      setErrorMsg(badCriteria ? t("scorecards.validation.scoreRange") : null);
      return;
    }
    setErrorMsg(null);
    save.mutate();
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isCreate ? t("scorecards.actions.create") : t("scorecards.actions.edit")}</DialogTitle>
            <DialogDescription>{t("scorecards.formDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormErrorSummary message={errorMsg} />
            {readOnly && (
              <div className="mb-3 rounded-lg border border-[var(--color-success)]/40 bg-[var(--color-success)]/10 px-3 py-2 text-[12.5px] text-[var(--color-foreground)]">
                {t("scorecards.closedNote")}
              </div>
            )}
            <FormGrid>
              <Field id="sc-supplier" span={6} label={t("scorecards.fields.supplier")} required error={fieldErrs.supplierId}>
                {isCreate ? (
                  <PartyPicker id="sc-supplier" role="Supplier" value={form.supplierId} onChange={(v) => set({ supplierId: v })} />
                ) : (
                  <Input id="sc-supplier" value={detail?.supplierName ?? ""} disabled readOnly />
                )}
              </Field>
              <Field id="sc-period" span={3} label={t("scorecards.fields.period")} required error={fieldErrs.periodLabel} hint={t("scorecards.fields.periodHint")}>
                <Input id="sc-period" value={form.periodLabel} maxLength={32} disabled={readOnly} placeholder="2026-Q2"
                  onChange={(e) => set({ periodLabel: e.target.value })} />
              </Field>
              <Field id="sc-start" span={3} label={t("scorecards.fields.periodStart")}>
                <Input id="sc-start" type="date" value={form.periodStart} disabled={readOnly} onChange={(e) => set({ periodStart: e.target.value })} />
              </Field>
              <Field id="sc-notes" span={12} label={t("scorecards.fields.notes")} error={fieldErrs.notes}>
                <Textarea id="sc-notes" rows={2} value={form.notes} disabled={readOnly} onChange={(e) => set({ notes: e.target.value })} />
              </Field>
            </FormGrid>

            {/* Live score */}
            <div className="mt-4 flex items-center gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 py-2">
              <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("scorecards.fields.score")}</span>
              <span className="font-display text-[22px] font-semibold tabular-nums text-[var(--color-foreground)]">{liveScore.toFixed(2)}</span>
              <EntityStatusBadge tone={gradeTone(liveGrade)}>{liveGrade}</EntityStatusBadge>
              <span className="text-[11.5px] text-[var(--color-muted-foreground)]">{t("scorecards.scoreHint")}</span>
            </div>

            {/* Criteria */}
            <div className="mt-4 space-y-2">
              <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("scorecards.criteria.title")}</h3>
              {form.criteria.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("scorecards.criteria.empty")}</p>}
              {form.criteria.map((c, i) => (
                <div key={c.kpiCode} className="grid grid-cols-12 items-center gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-2">
                  <div className="col-span-12 sm:col-span-4">
                    <span className="text-[13px] font-medium text-[var(--color-foreground)]">{c.kpiName}</span>
                    <span className="ml-1.5 text-[11px] text-[var(--color-muted-foreground)]">({c.weight}%)</span>
                  </div>
                  <div className="col-span-4 sm:col-span-2">
                    <Combobox id={`sc-score-${i}`} label={t("scorecards.criteria.score")} value={String(c.score)}
                      onChange={(v) => v && updateCriterion(i, { score: Number(v) })} disabled={readOnly}
                      options={[1, 2, 3, 4, 5].map((n) => ({ value: String(n), label: String(n) }))} />
                  </div>
                  <div className="col-span-8 sm:col-span-6">
                    <Input aria-label={t("scorecards.criteria.comment")} value={c.comment ?? ""} disabled={readOnly}
                      placeholder={t("scorecards.criteria.comment")} onChange={(e) => updateCriterion(i, { comment: e.target.value })} />
                  </div>
                </div>
              ))}
            </div>

            {detail && (
              <div className="mt-4 flex items-center gap-2 border-t border-[var(--color-border)] pt-3">
                <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("scorecards.fields.status")}:</span>
                <EntityStatusBadge tone={statusTone(detail.status)}>{t(`scorecards.status.${detail.status}`)}</EntityStatusBadge>
                {detail.isMutable && (
                  <Button type="button" perm={P.catalog.scorecards.manage} variant="outline" size="sm"
                    disabled={closeMut.isPending} onClick={() => closeMut.mutate()}>{t("scorecards.actions.close")}</Button>
                )}
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <FormActions
              secondary={
                <DialogClose asChild>
                  <Button type="button" variant="outline" disabled={save.isPending}>
                    <CancelIcon className="size-4" />{tc("actions.cancel")}
                  </Button>
                </DialogClose>
              }
              primary={
                !readOnly ? (
                  <Button type="submit" perm={P.catalog.scorecards.manage} disabled={save.isPending}>
                    <SaveIcon className="size-4" />
                    {save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}
                  </Button>
                ) : undefined
              }
            />
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteScorecardDialog({ state, onClose }: { state: Editor; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const row = state.mode === "delete" ? state.row : undefined;
  const del = useMutation({
    mutationFn: (id: string) => deleteScorecard(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: ["crm", "scorecards"] }); onClose(); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });
  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("scorecards.actions.delete")}</DialogTitle>
          <DialogDescription>{t("scorecards.deleteConfirm", { name: row ? `${row.supplierName ?? ""} ${row.periodLabel}` : "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}>{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => row && del.mutate(row.id)} disabled={del.isPending || !row}>
            {del.isPending ? tc("feedback.deleting") : t("scorecards.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────── Reports tab ─────────────────────────
function ReportsTab() {
  const { t } = useTranslation("crm");
  const rankingQ = useQuery({ queryKey: ["crm", "scorecards", "ranking"], queryFn: getSupplierRanking });
  const [supplierId, setSupplierId] = useState<string | null>(null);
  const trendQ = useQuery({ queryKey: ["crm", "scorecards", "trend", supplierId], queryFn: () => getSupplierScoreTrend(supplierId!), enabled: !!supplierId });

  const rankingData = (rankingQ.data ?? []).map((r) => ({ supplier: r.supplierName ?? "—", score: r.weightedScore }));
  const trendData = (trendQ.data ?? []).map((p) => ({ period: p.periodLabel, score: p.weightedScore }));

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("scorecards.reports.ranking")}</h3>
        {rankingData.length === 0 ? (
          <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("scorecards.reports.empty")}</p>
        ) : (
          <MakaChart title="" type="Bar" height="320px" xField="supplier" yField="score"
            series={[{ dataSource: rankingData, xName: "supplier", yName: "score", name: t("scorecards.fields.score") }]} />
        )}
      </div>

      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
          <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("scorecards.reports.trend")}</h3>
          <div className="w-64"><PartyPicker id="trend-supplier" role="Supplier" value={supplierId} onChange={setSupplierId} label={t("scorecards.fields.supplier")} /></div>
        </div>
        {!supplierId ? (
          <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("scorecards.reports.pickSupplier")}</p>
        ) : trendData.length === 0 ? (
          <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("scorecards.reports.noTrend")}</p>
        ) : (
          <MakaChart title="" type="Line" height="320px" xField="period" yField="score"
            series={[{ dataSource: trendData, xName: "period", yName: "score", name: t("scorecards.fields.score") }]} />
        )}
      </div>
    </div>
  );
}

// ───────────────────────── KPIs tab ─────────────────────────
type KpiEditor = { mode: "closed" } | { mode: "create" } | { mode: "edit"; kpi: ScorecardKpiDto };

function KpisTab() {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const { can } = usePerm();
  const [editor, setEditor] = useState<KpiEditor>({ mode: "closed" });

  const kpisQ = useQuery({ queryKey: ["crm", "scorecard-kpis", "all"], queryFn: () => getScorecardKpis(false) });
  const kpis = kpisQ.data ?? [];
  const totalWeight = kpis.filter((k) => k.isActive).reduce((s, k) => s + k.weight, 0);

  const seed = useMutation({
    mutationFn: seedDefaultKpis,
    onSuccess: (n) => { toast.success(t("scorecards.kpis.seeded", { count: n })); queryClient.invalidateQueries({ queryKey: ["crm", "scorecard-kpis"] }); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });
  const del = useMutation({
    mutationFn: (id: string) => deleteScorecardKpi(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: ["crm", "scorecard-kpis"] }); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  type Row = ScorecardKpiDto & { activeLabel: string };
  const rows: Row[] = kpis.map((k) => ({ ...k, activeLabel: k.isActive ? tc("status.active") : tc("status.inactive") }));
  const columns: ColumnModel[] = [
    { field: "name", headerText: t("scorecards.kpis.name"), minWidth: 200 },
    { field: "code", headerText: t("scorecards.kpis.code"), width: 140 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "weight", headerText: t("scorecards.kpis.weight"), width: 110, textAlign: "Center",
      template: ((r: Row) => <span className="tabular-nums">{r.weight}%</span>) as any },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "activeLabel", headerText: tc("status.active"), width: 110, textAlign: "Center",
      template: ((r: Row) => <EntityStatusBadge tone={r.isActive ? "success" : "default"}>{r.activeLabel}</EntityStatusBadge>) as any },
  ];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className={`text-[12.5px] ${totalWeight === 100 ? "text-[var(--color-muted-foreground)]" : "text-[var(--color-warning)]"}`}>
          {t("scorecards.kpis.totalWeight", { total: totalWeight })}
        </p>
        <div className="flex gap-2">
          {kpis.length === 0 && (
            <Button perm={P.catalog.scorecards.manage} variant="outline" disabled={seed.isPending} onClick={() => seed.mutate()}
              className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">{t("scorecards.kpis.seed")}</Button>
          )}
          <Button perm={P.catalog.scorecards.manage} onClick={() => setEditor({ mode: "create" })}
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"><Plus className="size-4" />{t("scorecards.kpis.create")}</Button>
        </div>
      </div>
      <MakaGridClient<Row>
        dataSource={rows} columns={columns} isLoading={kpisQ.isFetching}
        fileName="scorecard-kpis" entityName={t("scorecards.kpis.singular")}
        onRowClick={(r) => can(P.catalog.scorecards.manage) && setEditor({ mode: "edit", kpi: r })}
        permissions={{ edit: P.catalog.scorecards.manage, delete: P.catalog.scorecards.manage }}
        onEdit={(r) => setEditor({ mode: "edit", kpi: r })}
        onDelete={(r) => del.mutate(r.id)}
      />
      <KpiEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function KpiEditorDialog({ state, onClose }: { state: KpiEditor; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "create" || state.mode === "edit";
  const kpi = state.mode === "edit" ? state.kpi : undefined;

  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [weight, setWeight] = useState(0);
  const [sortOrder, setSortOrder] = useState(0);
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!isOpen) return;
    setCode(kpi?.code ?? ""); setName(kpi?.name ?? ""); setWeight(kpi?.weight ?? 0);
    setSortOrder(kpi?.sortOrder ?? 0); setIsActive(kpi?.isActive ?? true);
  }, [isOpen, kpi]);

  const save = useMutation({
    mutationFn: () => upsertScorecardKpi({ id: kpi?.id ?? null, code: code.trim(), name: name.trim(), weight, sortOrder, isActive }),
    onSuccess: () => { toast.success(kpi ? tc("feedback.updated") : tc("feedback.created")); queryClient.invalidateQueries({ queryKey: ["crm", "scorecard-kpis"] }); onClose(); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const canSubmit = !!code.trim() && !!name.trim() && weight >= 0 && weight <= 100;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={(e) => { e.preventDefault(); if (canSubmit) save.mutate(); }}>
          <DialogHeader>
            <DialogTitle>{kpi ? t("scorecards.kpis.edit") : t("scorecards.kpis.create")}</DialogTitle>
            <DialogDescription>{t("scorecards.kpis.formDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="kpi-code" span={4} label={t("scorecards.kpis.code")} required>
                <Input id="kpi-code" value={code} maxLength={64} disabled={!!kpi} onChange={(e) => setCode(e.target.value.toUpperCase())} className="font-mono uppercase" />
              </Field>
              <Field id="kpi-name" span={8} label={t("scorecards.kpis.name")} required>
                <Input id="kpi-name" value={name} maxLength={200} onChange={(e) => setName(e.target.value)} />
              </Field>
              <Field id="kpi-weight" span={4} label={t("scorecards.kpis.weight")} required>
                <Input id="kpi-weight" type="number" min={0} max={100} step="0.01" value={weight} onChange={(e) => setWeight(Number(e.target.value) || 0)} />
              </Field>
              <Field id="kpi-order" span={4} label={t("scorecards.kpis.sortOrder")}>
                <Input id="kpi-order" type="number" min={0} value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value) || 0)} />
              </Field>
              <div className="col-span-1 flex items-center sm:col-span-4">
                <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isActive} onCheckedChange={setIsActive} />{tc("status.active")}
                </label>
              </div>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline" disabled={save.isPending}>{tc("actions.cancel")}</Button></DialogClose>
            <Button type="submit" disabled={save.isPending || !canSubmit}>{save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
