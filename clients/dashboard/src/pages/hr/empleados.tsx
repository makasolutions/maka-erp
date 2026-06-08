import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { IdCard, Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { EntityPageHeader, EntityStatusBadge, Field, FormGrid, Combobox } from "@/components/list";
import { MakaGridClient } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { AddressEditor } from "@/components/party/AddressEditor";
import { ChannelEditor } from "@/components/party/ChannelEditor";
import { ContactEditor } from "@/components/party/ContactEditor";
import { EmployeeInfoEditor } from "@/components/hr/EmployeeInfoEditor";
import { emptyAddress, emptyContact, isContactBlank } from "@/components/party/PartyForm";
import { nitVerificationDigit } from "@/lib/nit";
import { describe, formatMoney } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";
import {
  createParty, getPartyById, updateParty,
  type PartyAddress, type PartyChannel, type PartyContact, type PartyWriteInput,
} from "@/api/parties";
import {
  createEmployee, deleteEmployee, emptyEmployeeData, getEmployeeByPartyId, getEmployees,
  updateEmployee, type EmployeeData, type EmployeeDto,
} from "@/api/hr";

const KEY = ["hr", "employees"] as const;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; row: EmployeeDto }
  | { mode: "delete"; row: EmployeeDto };

type IdentityForm = {
  identificationTypeCode: string | null;
  identificationNumber: string;
  verificationDigit: number | null;
  firstName: string;
  lastName: string;
  email: string;
  addresses: PartyAddress[];
  contacts: PartyContact[];
  channels: PartyChannel[];
};

function emptyIdentity(): IdentityForm {
  return {
    identificationTypeCode: "CC", identificationNumber: "", verificationDigit: null,
    firstName: "", lastName: "", email: "", addresses: [emptyAddress()], contacts: [emptyContact()], channels: [],
  };
}

function identityToPartyInput(v: IdentityForm): PartyWriteInput {
  const legalName = `${v.firstName.trim()} ${v.lastName.trim()}`.trim() || "—";
  return {
    identificationTypeCode: v.identificationTypeCode ?? "CC", identificationNumber: v.identificationNumber.trim(),
    verificationDigit: v.verificationDigit, kind: "Natural", legalName,
    firstName: v.firstName.trim() || null, lastName: v.lastName.trim() || null,
    roles: "Employee", tradeName: null, email: v.email.trim() || null, website: null,
    taxRegimeCode: null, fiscalResponsibilities: null, actividadEconomicaCiiuCode: null,
    status: "Active", stage: "Customer", leadScore: 0, sourceCode: null, marketingType: null,
    birthDate: null, genderCode: null, maritalStatusCode: null,
    hasCredit: false, creditLimit: null, creditDaysCode: null, creditBlocked: false, creditCurrency: null,
    notes: null, branchId: null, addresses: v.addresses,
    contacts: v.contacts.filter((c) => !isContactBlank(c)), channels: v.channels, team: [],
  };
}

export function EmpleadosPage() {
  const { t } = useTranslation("hr");
  const { can } = usePerm();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  useEffect(() => {
    const tmr = setTimeout(() => setDebounced(search.trim()), 250);
    return () => clearTimeout(tmr);
  }, [search]);

  const query = useQuery({
    queryKey: [...KEY, "list", debounced],
    queryFn: () => getEmployees({ search: debounced || undefined, pageSize: 200, sort: "-createdat" }),
    placeholderData: keepPreviousData,
  });

  const PayrollCell = (row: EmployeeDto) =>
    row.payrollEnabled
      ? <EntityStatusBadge tone="success">{t("employee.fields.payrollEnabled")}</EntityStatusBadge>
      : <span className="text-[var(--color-muted-foreground)]">—</span>;
  const SalaryCell = (row: EmployeeDto) =>
    <span className="text-[12.5px] tabular-nums text-[var(--color-foreground)]">{row.baseSalary != null ? formatMoney(row.baseSalary) : "—"}</span>;

  const columns: ColumnModel[] = useMemo(() => [
    { field: "positionCode", headerText: t("employee.fields.position"), width: 160 },
    { field: "laborDepartmentCode", headerText: t("employee.fields.laborDepartment"), width: 160 },
    { field: "branchCode", headerText: t("employee.fields.branch"), width: 130 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "payrollEnabled", headerText: t("employee.fields.payrollEnabled"), template: PayrollCell as any, width: 120, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "baseSalary", headerText: t("employee.fields.baseSalary"), template: SalaryCell as any, width: 150, textAlign: "Right" },
  ], [t]);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader icon={IdCard} title={t("title")} total={query.data?.totalCount ?? null}
        unit={t("singular")} description={t("description")}>
        <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder={t("filters.search")}
          aria-label={t("filters.search")} className="h-9 w-56" />
        <Button perm={P.hr.employees.create} onClick={() => setEditor({ mode: "create" })}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
          <Plus className="size-4" />{t("actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridClient<EmployeeDto>
        dataSource={query.data?.items ?? []}
        columns={columns}
        isLoading={query.isFetching}
        fileName="empleados"
        entityName={t("singular")}
        onRowClick={(row) => can(P.hr.employees.update) && setEditor({ mode: "edit", row })}
        permissions={{ edit: P.hr.employees.update, delete: P.hr.employees.delete }}
        onEdit={(row) => setEditor({ mode: "edit", row })}
        onDelete={(row) => setEditor({ mode: "delete", row })}
      />

      <EmpleadoEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteEmpleadoDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

type TabId = "id" | "contacts" | "employee";

function EmpleadoEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("hr");
  const { t: tp } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isCreate = state.mode === "create";
  const editRow = state.mode === "edit" ? state.row : undefined;
  const isOpen = isCreate || state.mode === "edit";

  const [tab, setTab] = useState<TabId>("id");
  const [identity, setIdentity] = useState<IdentityForm>(emptyIdentity());
  const [emp, setEmp] = useState<EmployeeData>(emptyEmployeeData());
  const [employeeId, setEmployeeId] = useState<string | null>(null);

  const partyQ = useQuery({
    queryKey: ["parties", "detail", editRow?.partyId],
    queryFn: () => getPartyById(editRow!.partyId),
    enabled: !!editRow,
  });
  const empQ = useQuery({
    queryKey: ["hr", "employee", editRow?.partyId],
    queryFn: () => getEmployeeByPartyId(editRow!.partyId),
    enabled: !!editRow,
  });

  useEffect(() => {
    if (!isOpen) return;
    setTab("id");
    if (isCreate) { setIdentity(emptyIdentity()); setEmp(emptyEmployeeData()); setEmployeeId(null); return; }
    if (partyQ.data) {
      const d = partyQ.data;
      setIdentity({
        identificationTypeCode: d.identificationTypeCode, identificationNumber: d.identificationNumber,
        verificationDigit: d.verificationDigit ?? null, firstName: d.firstName ?? "", lastName: d.lastName ?? "",
        email: d.email ?? "", addresses: d.addresses, contacts: d.contacts, channels: d.channels,
      });
    }
    if (empQ.data) { setEmp(empQ.data.data); setEmployeeId(empQ.data.id); }
  }, [isOpen, isCreate, partyQ.data, empQ.data]);

  const save = useMutation({
    mutationFn: async () => {
      const input = identityToPartyInput(identity);
      if (isCreate) {
        const partyId = await createParty(input);
        await createEmployee(partyId, emp);
      } else {
        await updateParty(editRow!.partyId, input);
        if (employeeId) await updateEmployee(employeeId, emp);
        else await createEmployee(editRow!.partyId, emp);
      }
    },
    onSuccess: () => {
      toast.success(isCreate ? tc("feedback.created") : tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: KEY });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const idOk = !!identity.identificationTypeCode && !!identity.identificationNumber.trim()
    && !!identity.firstName.trim() && !!identity.lastName.trim() && identity.addresses.length >= 1;
  const contactsOk = identity.contacts.every((c) => isContactBlank(c) || (!!(c.email ?? "").trim() && !!(c.cell ?? "").trim()));
  const canSubmit = idOk && contactsOk;
  const onSubmit = (e: FormEvent<HTMLFormElement>) => { e.preventDefault(); if (canSubmit) save.mutate(); };

  const setId = (patch: Partial<IdentityForm>) => setIdentity((v) => ({ ...v, ...patch }));
  const isNit = (identity.identificationTypeCode ?? "") === "NIT";

  const tabs: { id: TabId; label: string }[] = [
    { id: "id", label: tp("parties.tabs.identity") },
    { id: "contacts", label: tp("parties.tabs.contacts") },
    { id: "employee", label: t("employee.tab") },
  ];

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isCreate ? t("actions.create") : t("actions.edit")}</DialogTitle>
            <DialogDescription>{t("formDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <div className="space-y-4">
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

              <div className="min-h-[480px]">
              {tab === "id" && (
                <div className="space-y-6">
                  <FormGrid>
                    <Field id="emp-idtype" span={3} label={tp("parties.fields.idType")} required>
                      <BasicRecordSelect id="emp-idtype" tableCode="IdentificationType" label={tp("parties.fields.idType")}
                        value={identity.identificationTypeCode} onChange={(c) => setId({ identificationTypeCode: c })} disabled={!isCreate} />
                    </Field>
                    <Field id="emp-idnum" span={4} label={tp("parties.fields.idNumber")} required>
                      <Input id="emp-idnum" value={identity.identificationNumber} disabled={!isCreate} className="font-mono"
                        onChange={(e) => setId({ identificationNumber: e.target.value })} />
                    </Field>
                    <Field id="emp-dv" span={2} label={tp("parties.fields.dv")}>
                      <div className="flex gap-1">
                        <Input id="emp-dv" type="number" className="font-mono" value={identity.verificationDigit ?? ""}
                          onChange={(e) => setId({ verificationDigit: e.target.value === "" ? null : Number(e.target.value) })} />
                        {isNit && (
                          <Button type="button" variant="outline" size="sm"
                            onClick={() => setId({ verificationDigit: nitVerificationDigit(identity.identificationNumber) })}>DV</Button>
                        )}
                      </div>
                    </Field>
                    <Field id="emp-kind" span={3} label={tp("parties.fields.kind")}>
                      <Combobox id="emp-kind" label={tp("parties.fields.kind")} value="Natural" disabled
                        options={[{ value: "Natural", label: tp("parties.kind.Natural") }]} onChange={() => {}} />
                    </Field>
                    <Field id="emp-first" span={6} label={tp("parties.fields.firstName")} required>
                      <Input id="emp-first" value={identity.firstName} onChange={(e) => setId({ firstName: e.target.value })} />
                    </Field>
                    <Field id="emp-last" span={6} label={tp("parties.fields.lastName")} required>
                      <Input id="emp-last" value={identity.lastName} onChange={(e) => setId({ lastName: e.target.value })} />
                    </Field>
                    <Field id="emp-email" span={6} label={tp("parties.fields.email")}>
                      <Input id="emp-email" type="email" value={identity.email} onChange={(e) => setId({ email: e.target.value })} />
                    </Field>
                  </FormGrid>
                  <div className="border-t border-[var(--color-border)] pt-4">
                    <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{tp("parties.address.title")}<span className="ml-1 text-[var(--color-destructive)]">*</span></h3>
                    <AddressEditor value={identity.addresses} onChange={(a) => setId({ addresses: a })} />
                  </div>
                  <div className="border-t border-[var(--color-border)] pt-4">
                    <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{tp("parties.channel.title")}</h3>
                    <ChannelEditor value={identity.channels} onChange={(c) => setId({ channels: c })} />
                  </div>
                </div>
              )}

              {tab === "contacts" && (
                <ContactEditor value={identity.contacts} onChange={(c) => setId({ contacts: c })} />
              )}

              {tab === "employee" && (
                <EmployeeInfoEditor value={emp} onChange={setEmp} />
              )}
              </div>
            </div>
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

function DeleteEmpleadoDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("hr");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const row = state.mode === "delete" ? state.row : undefined;

  const del = useMutation({
    mutationFn: (id: string) => deleteEmployee(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: KEY }); onClose(); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("actions.delete")}</DialogTitle>
          <DialogDescription>{t("deleteConfirm")}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}>{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => row && del.mutate(row.id)} disabled={del.isPending}>
            {del.isPending ? tc("feedback.saving") : t("actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
