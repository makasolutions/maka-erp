import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, Plus } from "lucide-react";
import { toast } from "sonner";
import {
  createParty, deleteParty, getPartyById, hasRole, searchParties, updateParty,
  type PartyDto, type PartyStatus,
} from "@/api/parties";
import { Button } from "@/components/ui/button";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { EntityPageHeader, EntityStatusBadge, EntityFilterPill } from "@/components/list";
import { MakaFilterField, MakaFilterInput, MakaGridClient, MakaGridFilters } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import {
  PartyForm, emptyPartyForm, partyFormFromDetail, partyFormToInput, type PartyFormValue,
} from "@/components/party/PartyForm";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const KEY = ["crm", "parties"] as const;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; id: string }
  | { mode: "delete"; party: PartyDto };

function NameCell(row: PartyDto) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.legalName}</div>
      <code className="text-[11px] text-[var(--color-muted-foreground)]">{row.identificationTypeCode} {row.identificationNumber}{row.verificationDigit != null ? `-${row.verificationDigit}` : ""}</code>
    </div>
  );
}

export function PartiesPage() {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [debouncedName, setDebouncedName] = useState("");
  const [roleFilter, setRoleFilter] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  useEffect(() => {
    const tmr = setTimeout(() => setDebouncedName(nameFilter.trim()), 250);
    return () => clearTimeout(tmr);
  }, [nameFilter]);

  const params = useMemo(() => ({
    search: debouncedName || undefined,
    role: (roleFilter as "Customer" | "Supplier" | null) ?? undefined,
    status: (statusFilter as PartyStatus | null) ?? undefined,
    pageSize: 200, sort: "legalName",
  }), [debouncedName, roleFilter, statusFilter]);

  const query = useQuery({
    queryKey: [...KEY, "list", params],
    queryFn: () => searchParties(params),
    placeholderData: keepPreviousData,
  });

  const RolesCell = (row: PartyDto) => (
    <div className="flex flex-wrap gap-1">
      {hasRole(row.roles, "Customer") && <EntityStatusBadge tone="info">{t("parties.role.customer")}</EntityStatusBadge>}
      {hasRole(row.roles, "Supplier") && <EntityStatusBadge tone="success">{t("parties.role.supplier")}</EntityStatusBadge>}
      {!hasRole(row.roles, "Customer") && !hasRole(row.roles, "Supplier") && <span className="text-[var(--color-muted-foreground)]">—</span>}
    </div>
  );
  const StageCell = (row: PartyDto) => <span className="text-[12.5px] text-[var(--color-foreground)]">{t(`parties.stage.${row.stage}`)}</span>;
  const CityCell = (row: PartyDto) => <span className="text-[12.5px] text-[var(--color-muted-foreground)]">{row.city ?? "—"}</span>;

  const columns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "legalName", headerText: t("parties.fields.party"), template: NameCell as any, minWidth: 240 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "roles", headerText: t("parties.fields.roles"), template: RolesCell as any, width: 160, textAlign: "Center", allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "stage", headerText: t("parties.fields.stage"), template: StageCell as any, width: 130, allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "city", headerText: t("parties.fields.city"), template: CityCell as any, width: 140, allowSorting: false },
  ], [t]);

  const resetFilters = () => { setNameFilter(""); setDebouncedName(""); setRoleFilter(null); setStatusFilter(null); };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Building2}
        title={t("parties.title")}
        total={query.data?.totalCount ?? null}
        unit={t("parties.singular")}
        description={t("parties.description")}
      >
        <Button variant="outline" onClick={() => setPanelOpen((x) => !x)} aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">{tc("gridFilters.panelToggle")}</Button>
        <Button perm={P.parties.create} onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none">
          <Plus className="size-4" />{t("parties.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("parties.fields.party")} className="grow">
              <MakaFilterInput value={nameFilter} onChange={setNameFilter}
                placeholder={t("parties.filters.searchPlaceholder")} ariaLabel={t("parties.fields.party")} className="min-w-56" />
            </MakaFilterField>
            <MakaFilterField label={t("parties.fields.roles")}>
              <EntityFilterPill<string | null>
                label={t("parties.fields.roles")} value={roleFilter} onChange={setRoleFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "Customer", label: t("parties.role.customer") },
                  { value: "Supplier", label: t("parties.role.supplier") },
                ]} />
            </MakaFilterField>
            <MakaFilterField label={t("parties.fields.status")}>
              <EntityFilterPill<string | null>
                label={t("parties.fields.status")} value={statusFilter} onChange={setStatusFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "Active", label: t("parties.status.Active") },
                  { value: "Inactive", label: t("parties.status.Inactive") },
                  { value: "Prospect", label: t("parties.status.Prospect") },
                ]} />
            </MakaFilterField>
          </>
        }
      />

      <MakaGridClient<PartyDto>
        dataSource={query.data?.items ?? []}
        columns={columns}
        isLoading={query.isFetching}
        fileName="terceros"
        entityName={t("parties.singular")}
        onRowClick={(row) => can(P.parties.update) && setEditor({ mode: "edit", id: row.id })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.parties.update, delete: P.parties.delete }}
        onEdit={(row) => setEditor({ mode: "edit", id: row.id })}
        onDelete={(row) => setEditor({ mode: "delete", party: row })}
      />

      <PartyEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeletePartyDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function PartyEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isCreate = state.mode === "create";
  const editId = state.mode === "edit" ? state.id : undefined;
  const isOpen = isCreate || state.mode === "edit";

  const [form, setForm] = useState<PartyFormValue>(emptyPartyForm());

  const detailQ = useQuery({
    queryKey: ["crm", "parties", "detail", editId],
    queryFn: () => getPartyById(editId!),
    enabled: !!editId,
  });

  useEffect(() => {
    if (!isOpen) return;
    if (isCreate) setForm(emptyPartyForm());
    else if (detailQ.data) setForm(partyFormFromDetail(detailQ.data));
  }, [isOpen, isCreate, detailQ.data]);

  const save = useMutation({
    mutationFn: async () => {
      const input = partyFormToInput(form);
      if (isCreate) await createParty(input);
      else await updateParty(editId!, input);
    },
    onSuccess: () => {
      toast.success(isCreate ? tc("feedback.created") : tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: KEY });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const canSubmit = !!form.identificationTypeCode && !!form.identificationNumber.trim() && !!form.legalName.trim();
  const onSubmit = (e: FormEvent<HTMLFormElement>) => { e.preventDefault(); if (canSubmit) save.mutate(); };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isCreate ? t("parties.actions.create") : t("parties.actions.edit")}</DialogTitle>
            <DialogDescription>{t("parties.formDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <PartyForm value={form} onChange={setForm} isCreate={isCreate} />
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

function DeletePartyDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const party = state.mode === "delete" ? state.party : undefined;

  const del = useMutation({
    mutationFn: (id: string) => deleteParty(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: KEY }); onClose(); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("parties.actions.delete")}</DialogTitle>
          <DialogDescription>{t("parties.deleteConfirm", { name: party?.legalName ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}>{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => party && del.mutate(party.id)} disabled={del.isPending}>
            {del.isPending ? tc("feedback.saving") : t("parties.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
