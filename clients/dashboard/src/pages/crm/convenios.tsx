import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Check, Handshake, Pause, Play, Plus, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import {
  changeAgreementStatus, createAgreement, deleteAgreement, evaluateAgreement, getAgreementById,
  getAgreements, updateAgreement,
  type AgreementDto, type AgreementRuleInput, type AgreementRuleType, type AgreementStatus,
  type AgreementType, type AgreementResponsible, type AgreementWriteInput, type RuleEvaluationResult,
} from "@/api/agreements";
import { getPriceLists } from "@/api/catalog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import {
  Combobox, EntityFilterPill, EntityPageHeader, EntityStatusBadge, Field, FormErrorSummary, FormGrid, FormTabs,
  type FormTab,
} from "@/components/list";
import { MakaGridClient, MakaGridFilters, MakaFilterField, MakaFilterInput, MakaDatePicker } from "@/components/maka";
import { PartyPicker } from "@/components/party/PartyPicker";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const TYPES: AgreementType[] = ["ProveedorUnico", "ProveedorModelo", "DistribuidorAprobado", "ModeloPendiente"];
const STATUSES: AgreementStatus[] = ["Borrador", "Vigente", "Suspendido", "Terminado"];
const RESPONSIBLES: AgreementResponsible[] = ["Proveedor", "Distribuidor", "Plataforma"];
const RULE_TYPES: AgreementRuleType[] = [
  "CompraMinimaMes", "CompraMinimaAnio", "AntiguedadMinimaMeses", "VendeAEmpresa", "VendeANatural",
  "DocumentoExigido", "CalificacionMinima", "SlaEntregaDias", "CumplimientoMinimo",
];
const NUMERIC_RULES = new Set<AgreementRuleType>([
  "CompraMinimaMes", "CompraMinimaAnio", "AntiguedadMinimaMeses", "CalificacionMinima", "SlaEntregaDias", "CumplimientoMinimo",
]);

function statusTone(s: AgreementStatus): "success" | "warning" | "default" | "danger" {
  return s === "Vigente" ? "success" : s === "Suspendido" ? "warning" : s === "Terminado" ? "danger" : "default";
}
function resultTone(r: RuleEvaluationResult): "success" | "danger" | "warning" {
  return r === "Cumple" ? "success" : r === "NoCumple" ? "danger" : "warning";
}

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; id: string }
  | { mode: "delete"; row: AgreementDto };

type AgreementRow = AgreementDto & { supplierLabel: string; typeLabel: string; statusLabel: string };

export function ConveniosPage() {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState<AgreementStatus | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const listQ = useQuery({
    queryKey: ["crm", "agreements", "list"],
    queryFn: () => getAgreements({ pageSize: 200, sort: "name" }),
    placeholderData: keepPreviousData,
  });
  const rows: AgreementRow[] = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    return (listQ.data?.items ?? [])
      .filter((a) => (!name || a.name.toLowerCase().includes(name)) && (!statusFilter || a.status === statusFilter))
      .map((a) => ({
        ...a,
        supplierLabel: a.supplierName ?? "—",
        typeLabel: t(`convenios.type.${a.agreementType}`),
        statusLabel: t(`convenios.status.${a.status}`),
      }));
  }, [listQ.data, nameFilter, statusFilter, t]);

  const columns: ColumnModel[] = useMemo(() => [
    { field: "name", headerText: t("convenios.fields.name"), minWidth: 200 },
    { field: "supplierLabel", headerText: t("convenios.fields.supplier"), minWidth: 180 },
    { field: "typeLabel", headerText: t("convenios.fields.type"), width: 150 },
    { field: "statusLabel", headerText: t("convenios.fields.status"), width: 120, template: ((r: AgreementRow) =>
        <EntityStatusBadge tone={statusTone(r.status)}>{r.statusLabel}</EntityStatusBadge>) as ColumnModel["template"], textAlign: "Center" },
    { field: "priceListName", headerText: t("convenios.fields.priceList"), width: 160 },
    { field: "ruleCount", headerText: t("convenios.fields.rules"), width: 90, textAlign: "Center" },
  ], [t]);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader icon={Handshake} title={t("convenios.title")} total={listQ.data?.totalCount ?? null}
        unit={t("convenios.singular")} description={t("convenios.description")}>
        <Button perm={P.catalog.agreements.manage} onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none">
          <Plus className="size-4" />{t("convenios.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters open={panelOpen} onClear={() => { setNameFilter(""); setStatusFilter(null); }}
        filters={
          <>
            <MakaFilterField label={t("convenios.fields.name")} className="grow">
              <MakaFilterInput value={nameFilter} onChange={setNameFilter}
                placeholder={t("convenios.filters.searchPlaceholder")} ariaLabel={t("convenios.fields.name")} className="min-w-48" />
            </MakaFilterField>
            <MakaFilterField label={t("convenios.fields.status")}>
              <EntityFilterPill<AgreementStatus | null> label={t("convenios.fields.status")} value={statusFilter}
                onChange={setStatusFilter}
                options={[{ value: null, label: tc("status.all") },
                  ...STATUSES.map((s) => ({ value: s, label: t(`convenios.status.${s}`) }))]} />
            </MakaFilterField>
          </>
        }
      />

      <MakaGridClient<AgreementRow>
        dataSource={rows} columns={columns} isLoading={listQ.isFetching}
        fileName="convenios" entityName={t("convenios.singular")}
        onRowClick={(row) => can(P.catalog.agreements.view) && setEditor({ mode: "edit", id: row.id })}
        permissions={{ edit: P.catalog.agreements.view, delete: P.catalog.agreements.manage }}
        onEdit={(row) => setEditor({ mode: "edit", id: row.id })}
        onDelete={(row) => setEditor({ mode: "delete", row })}
      />

      <AgreementEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteAgreementDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

type FormState = {
  name: string;
  supplierId: string | null;
  agreementType: AgreementType;
  priceListId: string | null;
  suggestedPriceListId: string | null;
  dispatchResponsible: AgreementResponsible;
  waybillResponsible: AgreementResponsible;
  settlementResponsible: AgreementResponsible;
  failedDeliveryPolicy: string;
  returnsPolicy: string;
  warrantyPolicy: string;
  validFrom: string;
  validTo: string;
  notes: string;
  rules: AgreementRuleInput[];
};

function emptyForm(): FormState {
  return {
    name: "", supplierId: null, agreementType: "ProveedorModelo", priceListId: null, suggestedPriceListId: null,
    dispatchResponsible: "Proveedor", waybillResponsible: "Plataforma", settlementResponsible: "Plataforma",
    failedDeliveryPolicy: "", returnsPolicy: "", warrantyPolicy: "",
    validFrom: new Date().toISOString().slice(0, 10), validTo: "", notes: "", rules: [],
  };
}

function AgreementEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isCreate = state.mode === "create";
  const editId = state.mode === "edit" ? state.id : undefined;
  const isOpen = isCreate || state.mode === "edit";

  const [form, setForm] = useState<FormState>(emptyForm());
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [distributorId, setDistributorId] = useState<string | null>(null);
  const [tab, setTab] = useState("general");
  const set = (patch: Partial<FormState>) => setForm((f) => ({ ...f, ...patch }));

  const detailQ = useQuery({
    queryKey: ["crm", "agreements", "detail", editId],
    queryFn: () => getAgreementById(editId!),
    enabled: !!editId,
  });
  const priceListsQ = useQuery({
    queryKey: ["catalog", "price-lists", "picker"],
    queryFn: () => getPriceLists({ pageSize: 200, sort: "name" }),
    enabled: isOpen,
    staleTime: 60_000,
  });
  const priceListOptions = (priceListsQ.data?.items ?? []).map((p) => ({ value: p.id, label: p.name }));

  const detail = detailQ.data;
  const readOnly = !!detail && !detail.isMutable;

  useEffect(() => {
    if (!isOpen) return;
    setErrorMsg(null);
    setDistributorId(null);
    setTab("general");
    if (isCreate) { setForm(emptyForm()); return; }
    if (detail) {
      setForm({
        name: detail.name, supplierId: detail.supplierId, agreementType: detail.agreementType,
        priceListId: detail.priceListId ?? null, suggestedPriceListId: detail.suggestedPriceListId ?? null,
        dispatchResponsible: detail.dispatchResponsible, waybillResponsible: detail.waybillResponsible,
        settlementResponsible: detail.settlementResponsible,
        failedDeliveryPolicy: detail.failedDeliveryPolicy ?? "", returnsPolicy: detail.returnsPolicy ?? "",
        warrantyPolicy: detail.warrantyPolicy ?? "", validFrom: detail.validFrom.slice(0, 10),
        validTo: detail.validTo ? detail.validTo.slice(0, 10) : "", notes: detail.notes ?? "",
        rules: detail.rules.map((r) => ({ ruleType: r.ruleType, numericValue: r.numericValue, boolValue: r.boolValue, textValue: r.textValue, isMandatory: r.isMandatory })),
      });
    }
  }, [isOpen, isCreate, detail]);

  const toInput = (): AgreementWriteInput => ({
    name: form.name.trim(), supplierId: form.supplierId ?? "", agreementType: form.agreementType,
    priceListId: form.priceListId, suggestedPriceListId: form.suggestedPriceListId,
    dispatchResponsible: form.dispatchResponsible, waybillResponsible: form.waybillResponsible,
    settlementResponsible: form.settlementResponsible,
    failedDeliveryPolicy: form.failedDeliveryPolicy.trim() || null, returnsPolicy: form.returnsPolicy.trim() || null,
    warrantyPolicy: form.warrantyPolicy.trim() || null, validFrom: form.validFrom,
    validTo: form.validTo || null, notes: form.notes.trim() || null, rules: form.rules,
  });

  const save = useMutation({
    mutationFn: async () => {
      const input = toInput();
      if (isCreate) await createAgreement(input);
      else await updateAgreement(editId!, input);
    },
    onSuccess: () => {
      toast.success(isCreate ? tc("feedback.created") : tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["crm", "agreements"] });
      onClose();
    },
    onError: (e) => { setErrorMsg(describe(e)); toast.error(tc("feedback.saveFailed"), { description: describe(e) }); },
  });

  const statusMut = useMutation({
    mutationFn: (s: AgreementStatus) => changeAgreementStatus(editId!, s),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["crm", "agreements"] });
    },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const evalQ = useQuery({
    queryKey: ["crm", "agreements", "evaluate", editId, distributorId],
    queryFn: () => evaluateAgreement(editId!, distributorId!),
    enabled: !!editId && !!distributorId,
  });

  const validate = (): string | null => {
    if (!form.name.trim()) return t("convenios.validation.nameRequired");
    if (!form.supplierId) return t("convenios.validation.supplierRequired");
    if (form.validTo && form.validTo < form.validFrom) return t("convenios.validation.dateRange");
    for (const r of form.rules) {
      if (NUMERIC_RULES.has(r.ruleType) && (r.numericValue == null || r.numericValue < 0)) return t("convenios.validation.ruleNumeric");
      if (r.ruleType === "DocumentoExigido" && !(r.textValue ?? "").trim()) return t("convenios.validation.ruleDoc");
    }
    return null;
  };

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const err = validate();
    if (err) { setErrorMsg(err); return; }
    setErrorMsg(null);
    save.mutate();
  };

  // Rules as a fixed list of every possible rule type (§18.6): toggle to include +
  // fill its value in place. No add/remove + type-picker dance — the user sees the
  // whole universe of conditions at once and activates the ones that apply.
  const ruleByType = useMemo(
    () => new Map(form.rules.map((r) => [r.ruleType, r] as const)),
    [form.rules],
  );
  const toggleRule = (type: AgreementRuleType) =>
    set(
      ruleByType.has(type)
        ? { rules: form.rules.filter((r) => r.ruleType !== type) }
        : { rules: [...form.rules, { ruleType: type, numericValue: NUMERIC_RULES.has(type) ? 0 : null, boolValue: null, textValue: null, isMandatory: true }] },
    );
  const patchRule = (type: AgreementRuleType, patch: Partial<AgreementRuleInput>) =>
    set({ rules: form.rules.map((r) => (r.ruleType === type ? { ...r, ...patch } : r)) });

  // Per-tab error flags (drive the red dot on each tab after a failed submit).
  const showErrors = !!errorMsg;
  const generalHasError = !form.name.trim() || !form.supplierId || (!!form.validTo && form.validTo < form.validFrom);
  const rulesHaveError = form.rules.some(
    (r) =>
      (NUMERIC_RULES.has(r.ruleType) && (r.numericValue == null || r.numericValue < 0)) ||
      (r.ruleType === "DocumentoExigido" && !(r.textValue ?? "").trim()),
  );
  const errDot = <span aria-hidden className="ml-1 inline-block size-1.5 rounded-full bg-[var(--color-destructive)]" />;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isCreate ? t("convenios.actions.create") : t("convenios.actions.edit")}</DialogTitle>
            <DialogDescription>{t("convenios.formDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormErrorSummary message={errorMsg} />
            {readOnly && (
              <div className="mb-3 rounded-lg border border-[var(--color-warning)]/40 bg-[var(--color-warning)]/10 px-3 py-2 text-[12.5px] text-[var(--color-foreground)]">
                {t("convenios.terminatedNote")}
              </div>
            )}
            <FormTabs
              active={tab}
              onChange={setTab}
              tabs={[
                {
                  id: "general",
                  label: t("convenios.tabs.general"),
                  badge: showErrors && generalHasError ? errDot : undefined,
                  content: (
                    <FormGrid>
                      <Field id="ag-name" span={4} label={t("convenios.fields.name")} required>
                        <Input id="ag-name" value={form.name} maxLength={200} disabled={readOnly}
                          onChange={(e) => set({ name: e.target.value })} />
                      </Field>
                      <Field id="ag-type" span={4} label={t("convenios.fields.type")} required>
                        <Combobox id="ag-type" label={t("convenios.fields.type")} value={form.agreementType}
                          onChange={(v) => v && set({ agreementType: v as AgreementType })} disabled={readOnly}
                          options={TYPES.map((x) => ({ value: x, label: t(`convenios.type.${x}`) }))} />
                      </Field>
                      <Field id="ag-supplier" span={4} label={t("convenios.fields.supplier")} required>
                        {isCreate ? (
                          <PartyPicker id="ag-supplier" partyRole="Supplier" value={form.supplierId} onChange={(v) => set({ supplierId: v })} />
                        ) : (
                          <Input id="ag-supplier" value={detail?.supplierName ?? ""} disabled readOnly />
                        )}
                      </Field>
                      <Field id="ag-pl" span={4} label={t("convenios.fields.priceList")} hint={t("convenios.fields.priceListHint")}>
                        <Combobox id="ag-pl" label={t("convenios.fields.priceList")} value={form.priceListId}
                          onChange={(v) => set({ priceListId: v })} options={priceListOptions} searchable clearable disabled={readOnly} />
                      </Field>
                      <Field id="ag-spl" span={4} label={t("convenios.fields.suggestedPriceList")}>
                        <Combobox id="ag-spl" label={t("convenios.fields.suggestedPriceList")} value={form.suggestedPriceListId}
                          onChange={(v) => set({ suggestedPriceListId: v })} options={priceListOptions} searchable clearable disabled={readOnly} />
                      </Field>
                      <Field id="ag-disp" span={4} label={t("convenios.fields.dispatch")}>
                        <Combobox id="ag-disp" label={t("convenios.fields.dispatch")} value={form.dispatchResponsible}
                          onChange={(v) => v && set({ dispatchResponsible: v as AgreementResponsible })} disabled={readOnly}
                          options={RESPONSIBLES.map((x) => ({ value: x, label: t(`convenios.responsible.${x}`) }))} />
                      </Field>
                      <Field id="ag-way" span={4} label={t("convenios.fields.waybill")}>
                        <Combobox id="ag-way" label={t("convenios.fields.waybill")} value={form.waybillResponsible}
                          onChange={(v) => v && set({ waybillResponsible: v as AgreementResponsible })} disabled={readOnly}
                          options={RESPONSIBLES.map((x) => ({ value: x, label: t(`convenios.responsible.${x}`) }))} />
                      </Field>
                      <Field id="ag-set" span={4} label={t("convenios.fields.settlement")}>
                        <Combobox id="ag-set" label={t("convenios.fields.settlement")} value={form.settlementResponsible}
                          onChange={(v) => v && set({ settlementResponsible: v as AgreementResponsible })} disabled={readOnly}
                          options={RESPONSIBLES.map((x) => ({ value: x, label: t(`convenios.responsible.${x}`) }))} />
                      </Field>
                      <Field id="ag-from" span={4} label={t("convenios.fields.validFrom")}>
                        <MakaDatePicker id="ag-from" value={form.validFrom || null} disabled={readOnly}
                          onChange={(iso) => set({ validFrom: iso ?? "" })} />
                      </Field>
                      <Field id="ag-to" span={4} label={t("convenios.fields.validTo")} hint={t("convenios.fields.validToHint")}>
                        <MakaDatePicker id="ag-to" value={form.validTo || null} disabled={readOnly}
                          min={form.validFrom ? new Date(form.validFrom) : undefined}
                          onChange={(iso) => set({ validTo: iso ?? "" })} />
                      </Field>
                    </FormGrid>
                  ),
                },
                {
                  id: "policies",
                  label: t("convenios.tabs.policies"),
                  content: (
                    <FormGrid>
                      <Field id="ag-failed" span={12} label={t("convenios.fields.failedDelivery")}>
                        <Textarea id="ag-failed" rows={8} value={form.failedDeliveryPolicy} disabled={readOnly}
                          onChange={(e) => set({ failedDeliveryPolicy: e.target.value })} />
                      </Field>
                      <Field id="ag-returns" span={12} label={t("convenios.fields.returns")}>
                        <Textarea id="ag-returns" rows={8} value={form.returnsPolicy} disabled={readOnly}
                          onChange={(e) => set({ returnsPolicy: e.target.value })} />
                      </Field>
                      <Field id="ag-warranty" span={12} label={t("convenios.fields.warranty")}>
                        <Textarea id="ag-warranty" rows={8} value={form.warrantyPolicy} disabled={readOnly}
                          onChange={(e) => set({ warrantyPolicy: e.target.value })} />
                      </Field>
                      <Field id="ag-notes" span={12} label={t("convenios.fields.notes")}>
                        <Textarea id="ag-notes" rows={8} value={form.notes} disabled={readOnly}
                          onChange={(e) => set({ notes: e.target.value })} />
                      </Field>
                    </FormGrid>
                  ),
                },
                {
                  id: "rules",
                  label: t("convenios.tabs.rules"),
                  badge: showErrors && rulesHaveError ? errDot : undefined,
                  content: (
                    <div className="space-y-2">
                      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("convenios.rules.allHint")}</p>
                      {RULE_TYPES.map((type) => {
                        const r = ruleByType.get(type);
                        const on = !!r;
                        return (
                          <div
                            key={type}
                            className={`grid grid-cols-12 items-center gap-3 rounded-lg border border-[var(--color-border)] p-3 ${on ? "bg-[var(--color-card)]" : "bg-[var(--color-background)]"}`}
                          >
                            <label className="col-span-12 flex items-center gap-2.5 sm:col-span-5">
                              <Switch checked={on} disabled={readOnly} onCheckedChange={() => toggleRule(type)} aria-label={t("convenios.rules.include")} />
                              <span className={`text-[13px] font-medium ${on ? "text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]"}`}>
                                {t(`convenios.ruleType.${type}`)}
                              </span>
                            </label>
                            <div className="col-span-8 sm:col-span-5">
                              {on && NUMERIC_RULES.has(type) && (
                                <Input type="number" min={0} aria-label={t("convenios.rules.value")} value={r?.numericValue ?? ""}
                                  disabled={readOnly} onChange={(e) => patchRule(type, { numericValue: e.target.value === "" ? null : Number(e.target.value) })} />
                              )}
                              {on && type === "DocumentoExigido" && (
                                <Input aria-label={t("convenios.rules.value")} value={r?.textValue ?? ""} disabled={readOnly}
                                  placeholder={t("convenios.rules.docPlaceholder")} onChange={(e) => patchRule(type, { textValue: e.target.value })} />
                              )}
                            </div>
                            <div className="col-span-4 sm:col-span-2">
                              {on && (
                                <label className="flex h-9 items-center gap-2 text-[12.5px] font-medium text-[var(--color-foreground)]">
                                  <Switch checked={r!.isMandatory} disabled={readOnly} onCheckedChange={(c) => patchRule(type, { isMandatory: c })} />
                                  {t("convenios.rules.mandatory")}
                                </label>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  ),
                },
                ...(detail
                  ? [
                      {
                        id: "status",
                        label: t("convenios.tabs.status"),
                        content: (
                          <div className="space-y-3">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("convenios.fields.status")}:</span>
                              <EntityStatusBadge tone={statusTone(detail.status)}>{t(`convenios.status.${detail.status}`)}</EntityStatusBadge>
                              {detail.isMutable && (
                                <div className="flex flex-wrap gap-1.5">
                                  {detail.status !== "Vigente" && (
                                    <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                                      disabled={statusMut.isPending} onClick={() => statusMut.mutate("Vigente")}><Play className="size-4" />{t("convenios.statusActions.activate")}</Button>
                                  )}
                                  {detail.status === "Vigente" && (
                                    <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                                      disabled={statusMut.isPending} onClick={() => statusMut.mutate("Suspendido")}><Pause className="size-4" />{t("convenios.statusActions.suspend")}</Button>
                                  )}
                                  <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                                    disabled={statusMut.isPending} onClick={() => statusMut.mutate("Terminado")}><Ban className="size-4" />{t("convenios.statusActions.terminate")}</Button>
                                </div>
                              )}
                            </div>
                            <h3 className="text-sm font-semibold text-[var(--color-foreground)]">{t("convenios.evaluation.title")}</h3>
                            <div className="max-w-md">
                              <PartyPicker id="ag-distributor" label={t("convenios.evaluation.distributor")} value={distributorId} onChange={setDistributorId} />
                            </div>
                            {evalQ.data && (
                              <div className="space-y-2">
                                <EntityStatusBadge tone={evalQ.data.eligible ? "success" : "danger"}>
                                  {evalQ.data.eligible ? t("convenios.evaluation.eligible") : t("convenios.evaluation.notEligible")}
                                </EntityStatusBadge>
                                <div className="space-y-1">
                                  {evalQ.data.rules.map((r, i) => (
                                    <div key={i} className="flex items-center justify-between rounded-md border border-[var(--color-border)] px-3 py-1.5 text-[12.5px]">
                                      <span className="text-[var(--color-foreground)]">{t(`convenios.ruleType.${r.ruleType}`)}{r.isMandatory ? " *" : ""}</span>
                                      <span className="flex items-center gap-2 text-[var(--color-muted-foreground)]">
                                        {r.detail}
                                        <EntityStatusBadge tone={resultTone(r.result)}>{t(`convenios.result.${r.result}`)}</EntityStatusBadge>
                                      </span>
                                    </div>
                                  ))}
                                  {evalQ.data.rules.length === 0 && (
                                    <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("convenios.evaluation.noRules")}</p>
                                  )}
                                </div>
                              </div>
                            )}
                          </div>
                        ),
                      } as FormTab,
                    ]
                  : []),
              ]}
            />
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline" disabled={save.isPending}><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
            {!readOnly && (
              <Button type="submit" perm={P.catalog.agreements.manage} disabled={save.isPending}>
                <Check className="size-4" />{save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}
              </Button>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteAgreementDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const row = state.mode === "delete" ? state.row : undefined;

  const del = useMutation({
    mutationFn: (id: string) => deleteAgreement(id),
    onSuccess: () => {
      toast.success(tc("feedback.deleted"));
      queryClient.invalidateQueries({ queryKey: ["crm", "agreements"] });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("convenios.actions.delete")}</DialogTitle>
          <DialogDescription>{t("convenios.deleteConfirm", { name: row?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => row && del.mutate(row.id)} disabled={del.isPending || !row}>
            <Trash2 className="size-4" />{del.isPending ? tc("feedback.deleting") : t("convenios.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
