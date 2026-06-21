import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Star, UserCog } from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";

import { MakaGrid } from "@/components/maka/MakaGrid";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { MakaDatePicker } from "@/components/maka/MakaDatePicker";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Field, FormGrid, Combobox, type ComboboxOption } from "@/components/list";
import { Input } from "@/components/ui/input";
import { P } from "@/auth/permissions";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { useBasicRecords } from "@/components/lookups/use-basic-records";
import { searchParties } from "@/api/parties";
import {
  getRelationshipsByTarget, getRelationshipsBySource, createRelationship, updateRelationship,
  setPrimaryRelationship, deleteRelationship,
  type PartyRelationshipDto, type NewPersonInput,
} from "@/api/relationships";
import {
  CustomFieldRenderer, validateCustomFieldValues, type CustomFieldValues,
} from "@/components/party/CustomFieldRenderer";
import { getCustomFieldDefinitions } from "@/api/custom-fields";
import { cn } from "@/lib/cn";

/** Contacto acumulado en memoria durante la CREACIÓN del tercero (Opción B). Se mapea a
 *  PartyRelationshipLineInput al guardar (la persona nueva se crea atómicamente en el backend). */
export type BufferContact = {
  key: string;
  sourcePartyId?: string | null;
  newPerson?: NewPersonInput | null;
  displayName: string;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  startDate?: string | null;
  endDate?: string | null;
  customFields?: CustomFieldValues | null;
};

/** Códigos de Tabla Básica que componen una relación (PR-2). */
const T_REL = "RelationshipType";
const T_FUN = "ContactFunction";
const T_POS = "Position";

/** Funciones DIAN que la "empresa completa" sugiere (§8.1 SPEC). REP_LEGAL es un RelationshipType. */
const REP_LEGAL = "REPRESENTANTE_LEGAL";

type ViewRow = {
  id: string;
  name: string;
  relationshipType: string;
  contactFunction: string;
  jobTitle: string;
  channel: string;
  primary: string;
  active: string;
  raw: Draft;
};

/** Borrador editable de un contacto, común a buffer y live. */
type Draft = {
  id?: string;                 // relationship id (live) o key (buffer)
  sourcePartyId?: string | null;
  newPerson?: NewPersonInput | null;
  displayName: string;
  relationshipTypeCode: string;
  contactFunctionCode?: string | null;
  jobTitleCode?: string | null;
  isPrimary: boolean;
  isActive: boolean;
  startDate?: string | null;
  endDate?: string | null;
  isPEP: boolean;
  channel?: string | null;
  customFields: CustomFieldValues;
};

type Props =
  | { mode: "buffer"; value: BufferContact[]; onChange: (v: BufferContact[]) => void; disabled?: boolean }
  | { mode: "live"; companyId: string; disabled?: boolean };

export function ContactList(props: Props) {
  const { t } = useTranslation("crm");
  const qc = useQueryClient();
  const disabled = props.disabled ?? false;

  const rel = useBasicRecords(T_REL);
  const fun = useBasicRecords(T_FUN);
  const pos = useBasicRecords(T_POS);
  const labelOf = (records: { code: string; value: string }[] | undefined, code?: string | null) =>
    (code ? records?.find((r) => r.code === code)?.value : null) ?? (code ?? "—");

  // ── Datos según modo ──────────────────────────────────────────────────────
  const liveQuery = useQuery({
    queryKey: ["parties", "relationships", "target", props.mode === "live" ? props.companyId : null],
    queryFn: () => getRelationshipsByTarget((props as { companyId: string }).companyId, true),
    enabled: props.mode === "live",
  });

  const drafts: Draft[] = useMemo(() => {
    if (props.mode === "buffer") {
      return props.value.map((c) => ({
        id: c.key, sourcePartyId: c.sourcePartyId, newPerson: c.newPerson, displayName: c.displayName,
        relationshipTypeCode: c.relationshipTypeCode, contactFunctionCode: c.contactFunctionCode,
        jobTitleCode: c.jobTitleCode, isPrimary: c.isPrimary, isActive: true,
        startDate: c.startDate, endDate: c.endDate, isPEP: false, customFields: c.customFields ?? {},
      }));
    }
    return (liveQuery.data ?? []).map((r: PartyRelationshipDto) => ({
      id: r.id, sourcePartyId: r.sourcePartyId, displayName: r.sourceName ?? "—",
      relationshipTypeCode: r.relationshipTypeCode, contactFunctionCode: r.contactFunctionCode,
      jobTitleCode: r.jobTitleCode, isPrimary: r.isPrimary, isActive: r.isActive,
      startDate: r.startDate, endDate: r.endDate, isPEP: r.sourceIsPEP, channel: r.sourcePrimaryChannel,
      customFields: r.customFieldsJson ? safeParse(r.customFieldsJson) : {},
    }));
  }, [props, liveQuery.data]);

  const rows: ViewRow[] = useMemo(() => drafts.map((d) => ({
    id: d.id!,
    name: d.displayName,
    relationshipType: labelOf(rel.data, d.relationshipTypeCode),
    contactFunction: labelOf(fun.data, d.contactFunctionCode),
    jobTitle: labelOf(pos.data, d.jobTitleCode),
    channel: d.channel || "—",
    primary: d.isPrimary ? t("parties.contacts.yes") : "—",
    active: d.isActive ? t("parties.contacts.yes") : t("parties.contacts.no"),
    raw: d,
  })), [drafts, rel.data, fun.data, pos.data, t]);

  const columns: ColumnModel[] = [
    { field: "name", headerText: t("parties.contact.fullName"), width: 200 },
    { field: "relationshipType", headerText: t("parties.contacts.cols.type"), width: 160 },
    { field: "contactFunction", headerText: t("parties.contacts.cols.function"), width: 150 },
    { field: "jobTitle", headerText: t("parties.contact.position"), width: 150 },
    { field: "channel", headerText: t("parties.contacts.cols.channel"), width: 170 },
    { field: "primary", headerText: t("parties.contacts.cols.primary"), width: 100 },
  ];

  // ── Editor ────────────────────────────────────────────────────────────────
  const [editing, setEditing] = useState<Draft | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);

  const openCreate = () => {
    setEditing({
      displayName: "", relationshipTypeCode: "", contactFunctionCode: null, jobTitleCode: null,
      isPrimary: drafts.length === 0, isActive: true, startDate: today(), endDate: null,
      isPEP: false, customFields: {},
    });
    setEditorOpen(true);
  };
  const openEdit = (row: ViewRow) => { setEditing({ ...row.raw }); setEditorOpen(true); };

  // ── Mutaciones (live) / mutaciones de buffer ───────────────────────────────
  const invalidate = () => qc.invalidateQueries({ queryKey: ["parties", "relationships"] });
  const createMut = useMutation({ mutationFn: createRelationship, onSuccess: invalidate });
  const updateMut = useMutation({
    mutationFn: (v: { id: string; input: Parameters<typeof updateRelationship>[1] }) => updateRelationship(v.id, v.input),
    onSuccess: invalidate,
  });
  const primaryMut = useMutation({
    mutationFn: (v: { id: string; isPrimary: boolean }) => setPrimaryRelationship(v.id, v.isPrimary),
    onSuccess: invalidate,
  });
  const deleteMut = useMutation({ mutationFn: deleteRelationship, onSuccess: invalidate });

  const saveDraft = (d: Draft) => {
    if (props.mode === "buffer") {
      const next = [...props.value];
      // Principal único en memoria (espejo del invariante backend).
      if (d.isPrimary) next.forEach((c) => { c.isPrimary = false; });
      const line: BufferContact = {
        key: d.id ?? crypto.randomUUID(),
        sourcePartyId: d.sourcePartyId ?? null, newPerson: d.newPerson ?? null, displayName: d.displayName,
        relationshipTypeCode: d.relationshipTypeCode, contactFunctionCode: d.contactFunctionCode,
        jobTitleCode: d.jobTitleCode, isPrimary: d.isPrimary, startDate: d.startDate, endDate: d.endDate,
        customFields: d.customFields,
      };
      const idx = next.findIndex((c) => c.key === line.key);
      if (idx >= 0) next[idx] = line; else next.push(line);
      props.onChange(next);
    } else if (d.id) {
      updateMut.mutate({ id: d.id, input: {
        relationshipTypeCode: d.relationshipTypeCode, contactFunctionCode: d.contactFunctionCode,
        jobTitleCode: d.jobTitleCode, startDate: d.startDate ?? today(), endDate: d.endDate, customFields: d.customFields,
      } });
      if (d.isPrimary) primaryMut.mutate({ id: d.id, isPrimary: true });
    } else {
      createMut.mutate({
        sourcePartyId: d.sourcePartyId ?? null, newPerson: d.newPerson ?? null, targetPartyId: (props as { companyId: string }).companyId,
        relationshipTypeCode: d.relationshipTypeCode, contactFunctionCode: d.contactFunctionCode,
        jobTitleCode: d.jobTitleCode, isPrimary: d.isPrimary, startDate: d.startDate, endDate: d.endDate,
        customFields: d.customFields,
      });
    }
    setEditorOpen(false);
  };

  const removeRow = (row: ViewRow) => {
    if (props.mode === "buffer") props.onChange(props.value.filter((c) => c.key !== row.id));
    else deleteMut.mutate(row.id);
  };
  const makePrimary = (row: ViewRow) => {
    if (props.mode === "buffer") props.onChange(props.value.map((c) => ({ ...c, isPrimary: c.key === row.id })));
    else primaryMut.mutate({ id: row.id, isPrimary: true });
  };

  // ── Completitud gobernada (C): NO bloquea, solo sugiere ────────────────────
  const completeness = useMemo(() => {
    const has = (pred: (d: Draft) => boolean) => drafts.some((d) => d.isActive && pred(d));
    return {
      repLegal: has((d) => d.relationshipTypeCode === REP_LEGAL),
      comercial: has((d) => d.contactFunctionCode === "COMERCIAL"),
      facturacion: has((d) => (d.contactFunctionCode ?? "").startsWith("FACTURACION")),
    };
  }, [drafts]);

  return (
    <div className="flex flex-col gap-3">
      <CompletenessBanner state={completeness} />

      <MakaGrid<ViewRow>
        dataSource={rows}
        columns={columns}
        entityName={t("parties.contact.singular").toLowerCase()}
        permissions={{ create: P.parties.relationships.manage, edit: P.parties.relationships.manage, delete: P.parties.relationships.manage }}
        onCreate={disabled ? undefined : openCreate}
        onEdit={disabled ? undefined : openEdit}
        onDelete={disabled ? undefined : removeRow}
        extraActions={disabled ? [] : [
          { key: "primary", label: t("parties.contacts.makePrimary"), icon: Star, perm: P.parties.relationships.manage, onClick: makePrimary },
        ]}
      />

      {editorOpen && editing && (
        <ContactEditorDialog
          open={editorOpen}
          draft={editing}
          isNew={!editing.id}
          onClose={() => setEditorOpen(false)}
          onSave={saveDraft}
        />
      )}
    </div>
  );
}

function CompletenessBanner({ state }: { state: { repLegal: boolean; comercial: boolean; facturacion: boolean } }) {
  const { t } = useTranslation("crm");
  const items = [
    { ok: state.repLegal, label: t("parties.completeness.repLegal") },
    { ok: state.comercial, label: t("parties.completeness.comercial") },
    { ok: state.facturacion, label: t("parties.completeness.facturacion") },
  ];
  if (items.every((i) => i.ok)) return null;
  return (
    <div className="rounded-lg border border-[var(--color-warning)]/40 bg-[var(--color-warning-subtle)] px-3 py-2.5 text-[13px]">
      <p className="font-medium text-[var(--color-foreground)]">{t("parties.completeness.title")}</p>
      <ul className="mt-1 flex flex-wrap gap-x-4 gap-y-1">
        {items.map((i) => (
          <li key={i.label} className={cn("flex items-center gap-1.5",
            i.ok ? "text-[var(--color-success)]" : "text-[var(--color-muted-foreground)]")}>
            <span aria-hidden>{i.ok ? "✓" : "•"}</span>{i.label}
          </li>
        ))}
      </ul>
    </div>
  );
}

// ─────────────────────────── Editor dialog ───────────────────────────

function ContactEditorDialog({
  open, draft, isNew, onClose, onSave,
}: { open: boolean; draft: Draft; isNew: boolean; onClose: () => void; onSave: (d: Draft) => void }) {
  const { t } = useTranslation("crm");
  const [d, setD] = useState<Draft>(draft);
  const [showErrors, setShowErrors] = useState(false);
  const set = (patch: Partial<Draft>) => setD((prev) => ({ ...prev, ...patch }));

  const { data: defs = [] } = useQuery({
    queryKey: ["parties", "custom-fields", "PartyRelationship"],
    queryFn: () => getCustomFieldDefinitions({ entityType: "PartyRelationship" }),
  });

  const personChosen = !!d.sourcePartyId || !!d.newPerson;
  const cfErrors = validateCustomFieldValues(defs, d.customFields, "minimal", t);
  const baseError =
    !personChosen ? t("parties.contacts.errPerson")
    : !d.relationshipTypeCode ? t("parties.contacts.errType")
    : null;

  const submit = () => {
    setShowErrors(true);
    if (baseError || Object.keys(cfErrors).length > 0) return;
    onSave(d);
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{isNew ? t("parties.contacts.add") : t("parties.contacts.edit")}</DialogTitle>
        </DialogHeader>
        <DialogBody>
          <FormGrid>
            {isNew && (
              <div className="col-span-1 sm:col-span-12">
                <ContactPicker
                  value={{ sourcePartyId: d.sourcePartyId ?? null, newPerson: d.newPerson ?? null, displayName: d.displayName }}
                  onChange={(p) => set({ sourcePartyId: p.sourcePartyId, newPerson: p.newPerson, displayName: p.displayName })}
                  error={showErrors && !personChosen ? t("parties.contacts.errPerson") : undefined}
                />
              </div>
            )}
            {!isNew && (
              <Field id="ce-name" span={12} label={t("parties.contact.fullName")}>
                <Input id="ce-name" value={d.displayName} disabled />
              </Field>
            )}

            <Field id="ce-type" span={4} label={t("parties.contacts.cols.type")} required
              error={showErrors && !d.relationshipTypeCode ? t("parties.contacts.errType") : undefined}>
              <BasicRecordSelect id="ce-type" tableCode={T_REL} label={t("parties.contacts.cols.type")}
                value={d.relationshipTypeCode || null} onChange={(c) => set({ relationshipTypeCode: c ?? "" })} />
            </Field>
            <Field id="ce-fun" span={4} label={t("parties.contacts.cols.function")}>
              <BasicRecordSelect id="ce-fun" tableCode={T_FUN} label={t("parties.contacts.cols.function")}
                value={d.contactFunctionCode ?? null} onChange={(c) => set({ contactFunctionCode: c })} />
            </Field>
            <Field id="ce-pos" span={4} label={t("parties.contact.position")}>
              <BasicRecordSelect id="ce-pos" tableCode={T_POS} label={t("parties.contact.position")}
                value={d.jobTitleCode ?? null} onChange={(c) => set({ jobTitleCode: c })} />
            </Field>

            <Field id="ce-start" span={4} label={t("parties.contacts.cols.start")}>
              <MakaDatePicker id="ce-start" value={d.startDate ?? null} max={toDate(d.endDate)}
                onChange={(iso) => set({ startDate: iso })} />
            </Field>
            <Field id="ce-end" span={4} label={t("parties.contacts.cols.end")}>
              <MakaDatePicker id="ce-end" value={d.endDate ?? null} min={toDate(d.startDate)}
                onChange={(iso) => set({ endDate: iso })} />
            </Field>
            <div className="col-span-1 flex items-end gap-4 sm:col-span-4">
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={d.isPrimary} onCheckedChange={(c) => set({ isPrimary: c })} />
                {t("parties.contacts.cols.primary")}
              </label>
            </div>

            {/* IsPEP de la persona (lectura) — relevante para representante legal */}
            {d.isPEP && (
              <div className="col-span-1 sm:col-span-12">
                <span className="inline-flex items-center gap-1.5 rounded-full bg-[var(--color-warning-subtle)] px-2.5 py-1 text-[12px] font-medium text-[var(--color-warning)]">
                  <UserCog className="size-3.5" /> {t("parties.contacts.isPEP")}
                </span>
              </div>
            )}

            {defs.length > 0 && (
              <div className="col-span-1 sm:col-span-12">
                <CustomFieldRenderer definitions={defs} values={d.customFields}
                  onChange={(slug, value) => set({ customFields: { ...d.customFields, [slug]: value } })}
                  mode="minimal" errors={showErrors ? cfErrors : undefined} />
              </div>
            )}
          </FormGrid>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild><Button variant="ghost">{t("parties.contacts.cancel", { defaultValue: "Cancelar" })}</Button></DialogClose>
          <Button onClick={submit}>{t("parties.contacts.save", { defaultValue: "Guardar" })}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────── Buscar-o-crear persona ───────────────────────────

type PickerValue = { sourcePartyId: string | null; newPerson: NewPersonInput | null; displayName: string };

function ContactPicker({
  value, onChange, error,
}: { value: PickerValue; onChange: (v: PickerValue) => void; error?: string }) {
  const { t } = useTranslation("crm");
  const [tab, setTab] = useState<"existing" | "new">(value.newPerson ? "new" : "existing");

  // Personas existentes (Kind=Natural) — el Combobox filtra client-side.
  const { data } = useQuery({
    queryKey: ["crm", "parties", "persons"],
    queryFn: () => searchParties({ kind: "Natural", pageSize: 200, sort: "legalName" }),
    staleTime: 60 * 1000,
  });
  const options: ComboboxOption[] = useMemo(
    () => (data?.items ?? []).map((p) => ({ value: p.id, label: p.legalName, hint: `${p.identificationTypeCode} ${p.identificationNumber}` })),
    [data],
  );

  // Persona nueva
  const np = value.newPerson;
  const setNew = (patch: Partial<NewPersonInput>) => {
    const merged: NewPersonInput = {
      identificationTypeCode: np?.identificationTypeCode ?? "",
      identificationNumber: np?.identificationNumber ?? "",
      verificationDigit: np?.verificationDigit ?? null,
      firstName: np?.firstName ?? null, lastName: np?.lastName ?? null, legalName: np?.legalName ?? null,
      ...patch,
    };
    const name = `${merged.firstName ?? ""} ${merged.lastName ?? ""}`.trim() || (merged.legalName ?? "");
    onChange({ sourcePartyId: null, newPerson: merged, displayName: name });
  };

  return (
    <div className="rounded-lg border border-[var(--color-border)] p-3">
      <div className="mb-2.5 inline-flex rounded-md border border-[var(--color-border)] p-0.5">
        {(["existing", "new"] as const).map((tb) => (
          <button key={tb} type="button" onClick={() => setTab(tb)}
            className={cn("rounded px-3 py-1 text-[12.5px] font-medium transition-colors",
              tab === tb ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]")}>
            {tb === "existing" ? t("parties.contacts.existing") : t("parties.contacts.newPerson")}
          </button>
        ))}
      </div>

      {tab === "existing" ? (
        <Combobox id="ce-person" label={t("parties.contact.singular")} value={value.sourcePartyId}
          onChange={(id) => onChange({ sourcePartyId: id, newPerson: null, displayName: options.find((o) => o.value === id)?.label ?? "" })}
          options={options} searchable clearable placeholder={t("parties.contacts.searchPerson")} />
      ) : (
        <FormGrid>
          <Field id="np-idtype" span={4} label={t("parties.fields.idType")} required>
            <BasicRecordSelect id="np-idtype" tableCode="IdentificationType" label={t("parties.fields.idType")}
              value={np?.identificationTypeCode || null} onChange={(c) => setNew({ identificationTypeCode: c ?? "" })} />
          </Field>
          <Field id="np-idnum" span={4} label={t("parties.fields.idNumber")} required>
            <Input id="np-idnum" inputMode="numeric" value={np?.identificationNumber ?? ""}
              onChange={(e) => setNew({ identificationNumber: e.target.value.replace(/\D/g, "") })} />
          </Field>
          <Field id="np-first" span={4} label={t("parties.fields.firstName")} required>
            <Input id="np-first" value={np?.firstName ?? ""} onChange={(e) => setNew({ firstName: e.target.value })} />
          </Field>
          <Field id="np-last" span={4} label={t("parties.fields.lastName")}>
            <Input id="np-last" value={np?.lastName ?? ""} onChange={(e) => setNew({ lastName: e.target.value })} />
          </Field>
        </FormGrid>
      )}
      {error && <p className="mt-1.5 text-[12px] text-[var(--color-destructive)]">{error}</p>}
    </div>
  );
}

// ─────────────────────────── Dirección B (persona→empresas) ───────────────────────────

export function PersonCompaniesList({ personId }: { personId: string }) {
  const { t } = useTranslation("crm");
  const rel = useBasicRecords(T_REL);
  const pos = useBasicRecords(T_POS);
  const { data = [], isLoading } = useQuery({
    queryKey: ["parties", "relationships", "source", personId],
    queryFn: () => getRelationshipsBySource(personId, false),
  });

  if (isLoading) return <p className="text-sm text-[var(--color-muted-foreground)]">…</p>;
  if (data.length === 0)
    return <p className="text-sm text-[var(--color-muted-foreground)]">{t("parties.contacts.noCompanies")}</p>;

  return (
    <ul className="flex flex-col divide-y divide-[var(--color-border)]">
      {data.map((r) => (
        <li key={r.id} className="flex items-center justify-between gap-3 py-2.5">
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-[var(--color-foreground)]">{r.targetName ?? "—"}</p>
            <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
              {(rel.data?.find((x) => x.code === r.relationshipTypeCode)?.value ?? r.relationshipTypeCode)}
              {r.jobTitleCode ? ` · ${pos.data?.find((x) => x.code === r.jobTitleCode)?.value ?? r.jobTitleCode}` : ""}
            </p>
          </div>
          {r.isPrimary && <Star className="size-4 shrink-0 text-[var(--color-warning)]" aria-label={t("parties.contacts.cols.primary")} />}
        </li>
      ))}
    </ul>
  );
}

// ─────────────────────────── helpers ───────────────────────────

function today(): string { return new Date().toISOString().slice(0, 10); }
function toDate(iso?: string | null): Date | undefined {
  if (!iso) return undefined;
  const [y, m, dd] = iso.split("-").map(Number);
  return Number.isFinite(y) ? new Date(y, (m ?? 1) - 1, dd ?? 1) : undefined;
}
function safeParse(json: string): CustomFieldValues {
  try { return JSON.parse(json) as CustomFieldValues; } catch { return {}; }
}
