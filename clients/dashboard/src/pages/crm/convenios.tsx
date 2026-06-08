import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Handshake, Plus, Trash2 } from "lucide-react";
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
  Combobox, EntityFilterPill, EntityPageHeader, EntityStatusBadge, Field, FormErrorSummary, FormGrid,
} from "@/components/list";
import { MakaGridClient, MakaGridFilters, MakaFilterField, MakaFilterInput } from "@/components/maka";
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
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "name", headerText: t("convenios.fields.name"), minWidth: 200 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "supplierLabel", headerText: t("convenios.fields.supplier"), minWidth: 180 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "typeLabel", headerText: t("convenios.fields.type"), width: 150 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "statusLabel", headerText: t("convenios.fields.status"), width: 120, template: ((r: AgreementRow) =>
        <EntityStatusBadge tone={statusTone(r.status)}>{r.statusLabel}</EntityStatusBadge>) as any, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "priceListName", headerText: t("convenios.fields.priceList"), width: 160 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
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

  const addRule = () => set({ rules: [...form.rules, { ruleType: "AntiguedadMinimaMeses", numericValue: 0, boolValue: null, textValue: null, isMandatory: true }] });
  const updateRule = (i: number, patch: Partial<AgreementRuleInput>) =>
    set({ rules: form.rules.map((r, idx) => (idx === i ? { ...r, ...patch } : r)) });
  const removeRule = (i: number) => set({ rules: form.rules.filter((_, idx) => idx !== i) });

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
            <FormGrid>
              <Field id="ag-name" span={8} label={t("convenios.fields.name")} required>
                <Input id="ag-name" value={form.name} maxLength={200} disabled={readOnly}
                  onChange={(e) => set({ name: e.target.value })} />
              </Field>
              <Field id="ag-type" span={4} label={t("convenios.fields.type")} required>
                <Combobox id="ag-type" label={t("convenios.fields.type")} value={form.agreementType}
                  onChange={(v) => v && set({ agreementType: v as AgreementType })} disabled={readOnly}
                  options={TYPES.map((x) => ({ value: x, label: t(`convenios.type.${x}`) }))} />
              </Field>
              <Field id="ag-supplier" span={6} label={t("convenios.fields.supplier")} required>
                {isCreate ? (
                  <PartyPicker id="ag-supplier" role="Supplier" value={form.supplierId} onChange={(v) => set({ supplierId: v })} />
                ) : (
                  <Input id="ag-supplier" value={detail?.supplierName ?? ""} disabled readOnly />
                )}
              </Field>
              <Field id="ag-pl" span={3} label={t("convenios.fields.priceList")} hint={t("convenios.fields.priceListHint")}>
                <Combobox id="ag-pl" label={t("convenios.fields.priceList")} value={form.priceListId}
                  onChange={(v) => set({ priceListId: v })} options={priceListOptions} searchable clearable disabled={readOnly} />
              </Field>
              <Field id="ag-spl" span={3} label={t("convenios.fields.suggestedPriceList")}>
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

              <Field id="ag-from" span={3} label={t("convenios.fields.validFrom")}>
                <Input id="ag-from" type="date" value={form.validFrom} disabled={readOnly}
                  onChange={(e) => set({ validFrom: e.target.value })} />
              </Field>
              <Field id="ag-to" span={3} label={t("convenios.fields.validTo")} hint={t("convenios.fields.validToHint")}>
                <Input id="ag-to" type="date" value={form.validTo} disabled={readOnly}
                  onChange={(e) => set({ validTo: e.target.value })} />
              </Field>

              <Field id="ag-failed" span={4} label={t("convenios.fields.failedDelivery")}>
                <Textarea id="ag-failed" rows={2} value={form.failedDeliveryPolicy} disabled={readOnly}
                  onChange={(e) => set({ failedDeliveryPolicy: e.target.value })} />
              </Field>
              <Field id="ag-returns" span={4} label={t("convenios.fields.returns")}>
                <Textarea id="ag-returns" rows={2} value={form.returnsPolicy} disabled={readOnly}
                  onChange={(e) => set({ returnsPolicy: e.target.value })} />
              </Field>
              <Field id="ag-warranty" span={4} label={t("convenios.fields.warranty")}>
                <Textarea id="ag-warranty" rows={2} value={form.warrantyPolicy} disabled={readOnly}
                  onChange={(e) => set({ warrantyPolicy: e.target.value })} />
              </Field>
              <Field id="ag-notes" span={12} label={t("convenios.fields.notes")}>
                <Textarea id="ag-notes" rows={2} value={form.notes} disabled={readOnly}
                  onChange={(e) => set({ notes: e.target.value })} />
              </Field>
            </FormGrid>

            {/* Rules editor */}
            <div className="mt-5 border-t border-[var(--color-border)] pt-4">
              <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("convenios.rules.title")}</h3>
              {form.rules.length === 0 && (
                <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("convenios.rules.empty")}</p>
              )}
              <div className="space-y-2">
                {form.rules.map((r, i) => (
                  <div key={i} className="grid grid-cols-12 items-end gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-2">
                    <div className="col-span-12 sm:col-span-5">
                      <Combobox id={`rule-type-${i}`} label={t("convenios.rules.type")} value={r.ruleType}
                        onChange={(v) => v && updateRule(i, { ruleType: v as AgreementRuleType })} disabled={readOnly}
                        options={RULE_TYPES.map((x) => ({ value: x, label: t(`convenios.ruleType.${x}`) }))} />
                    </div>
                    <div className="col-span-7 sm:col-span-4">
                      {NUMERIC_RULES.has(r.ruleType) ? (
                        <Input type="number" min={0} aria-label={t("convenios.rules.value")} value={r.numericValue ?? ""}
                          disabled={readOnly} onChange={(e) => updateRule(i, { numericValue: e.target.value === "" ? null : Number(e.target.value) })} />
                      ) : r.ruleType === "DocumentoExigido" ? (
                        <Input aria-label={t("convenios.rules.value")} value={r.textValue ?? ""} disabled={readOnly}
                          placeholder={t("convenios.rules.docPlaceholder")} onChange={(e) => updateRule(i, { textValue: e.target.value })} />
                      ) : (
                        <span className="text-[12px] text-[var(--color-muted-foreground)]">{t("convenios.rules.noValue")}</span>
                      )}
                    </div>
                    <div className="col-span-4 sm:col-span-2">
                      <label className="flex h-9 items-center gap-2 text-[12.5px] font-medium text-[var(--color-foreground)]">
                        <Switch checked={r.isMandatory} disabled={readOnly} onCheckedChange={(c) => updateRule(i, { isMandatory: c })} />
                        {t("convenios.rules.mandatory")}
                      </label>
                    </div>
                    <div className="col-span-1 flex justify-end">
                      <button type="button" disabled={readOnly} onClick={() => removeRule(i)} aria-label={tc("actions.delete")}
                        className="rounded p-1.5 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
                        <Trash2 className="size-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              {!readOnly && (
                <Button type="button" variant="outline" size="sm" className="mt-2" onClick={addRule}>
                  <Plus className="size-4" />{t("convenios.rules.add")}
                </Button>
              )}
            </div>

            {/* Status + evaluation (edit only) */}
            {detail && (
              <div className="mt-5 border-t border-[var(--color-border)] pt-4">
                <div className="mb-3 flex flex-wrap items-center gap-2">
                  <span className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">{t("convenios.fields.status")}:</span>
                  <EntityStatusBadge tone={statusTone(detail.status)}>{t(`convenios.status.${detail.status}`)}</EntityStatusBadge>
                  {detail.isMutable && (
                    <div className="flex flex-wrap gap-1.5">
                      {detail.status !== "Vigente" && (
                        <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                          disabled={statusMut.isPending} onClick={() => statusMut.mutate("Vigente")}>{t("convenios.statusActions.activate")}</Button>
                      )}
                      {detail.status === "Vigente" && (
                        <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                          disabled={statusMut.isPending} onClick={() => statusMut.mutate("Suspendido")}>{t("convenios.statusActions.suspend")}</Button>
                      )}
                      <Button type="button" perm={P.catalog.agreements.manage} variant="outline" size="sm"
                        disabled={statusMut.isPending} onClick={() => statusMut.mutate("Terminado")}>{t("convenios.statusActions.terminate")}</Button>
                    </div>
                  )}
                </div>

                <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{t("convenios.evaluation.title")}</h3>
                <div className="max-w-md">
                  <PartyPicker id="ag-distributor" label={t("convenios.evaluation.distributor")} value={distributorId} onChange={setDistributorId} />
                </div>
                {evalQ.data && (
                  <div className="mt-3 space-y-2">
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
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline" disabled={save.isPending}>{tc("actions.cancel")}</Button></DialogClose>
            {!readOnly && (
              <Button type="submit" perm={P.catalog.agreements.manage} disabled={save.isPending}>
                {save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}
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
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}>{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => row && del.mutate(row.id)} disabled={del.isPending || !row}>
            {del.isPending ? tc("feedback.deleting") : t("convenios.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
