import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Database, Eye, List, Plus, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import {
  createBasicTable, deleteBasicTable, getBasicTableById, searchBasicTables,
  updateBasicTable, upsertBasicRecords,
  type BasicRecordInput, type BasicTableDto,
} from "@/api/lookups";
import { Button } from "@/components/ui/button";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { EntityPageHeader, EntityStatusBadge, Field, FormGrid } from "@/components/list";
import { MakaFilterField, MakaFilterInput, MakaGridClient, MakaGridFilters } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const KEY = ["lookups", "tables"] as const;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; table: BasicTableDto }
  | { mode: "delete"; table: BasicTableDto }
  | { mode: "records"; table: BasicTableDto };

function NameCell(row: BasicTableDto) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      <code className="text-[11px] text-[var(--color-muted-foreground)]">{row.code}</code>
    </div>
  );
}

export function BasicTablesPage() {
  const { t } = useTranslation("lookups");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const canGlobal = can(P.lookups.global.manage);

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: [...KEY, "list"],
    queryFn: () => searchBasicTables({ pageSize: 200, sort: "name" }),
    placeholderData: keepPreviousData,
  });

  const rows = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    return (query.data?.items ?? []).filter(
      (x) => !name || x.name.toLowerCase().includes(name) || x.code.toLowerCase().includes(name),
    );
  }, [query.data, nameFilter]);

  const GlobalCell = (row: BasicTableDto) =>
    <EntityStatusBadge tone={row.isGlobal ? "info" : "default"}>{row.isGlobal ? t("global") : t("tenant")}</EntityStatusBadge>;
  const CountCell = (row: BasicTableDto) =>
    <span className="tabular-nums text-[13px] text-[var(--color-foreground)]">{row.recordCount}</span>;

  const columns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "name", headerText: t("fields.name"), template: NameCell as any, minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "isGlobal", headerText: t("fields.scope"), template: GlobalCell as any, width: 120, textAlign: "Center", allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "recordCount", headerText: t("fields.records"), template: CountCell as any, width: 110, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ], [t]);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Database}
        title={t("title")}
        total={query.data?.totalCount ?? null}
        unit={t("singular")}
        description={t("description")}
      >
        <Button variant="outline" onClick={() => setPanelOpen((v) => !v)} aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
          <Eye className="size-4" />{tc("gridFilters.panelToggle")}
        </Button>
        <Button perm={P.lookups.tables.create} onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none">
          <Plus className="size-4" />{t("actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={() => setNameFilter("")}
        filters={
          <MakaFilterField label={t("fields.name")} className="grow">
            <MakaFilterInput value={nameFilter} onChange={setNameFilter}
              placeholder={t("filters.namePlaceholder")} ariaLabel={t("fields.name")} className="min-w-48" />
          </MakaFilterField>
        }
      />

      <MakaGridClient<BasicTableDto>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="tablas-basicas"
        entityName={t("singular")}
        onRowClick={(row) => setEditor({ mode: "records", table: row })}
        permissions={{ edit: P.lookups.tables.update, delete: P.lookups.tables.delete }}
        onEdit={(row) => setEditor({ mode: "edit", table: row })}
        onDelete={(row) => setEditor({ mode: "delete", table: row })}
        extraActions={[{ key: "records", label: t("actions.records"), icon: List, perm: P.lookups.tables.view, onClick: (row) => setEditor({ mode: "records", table: row }) }]}
      />

      <TableEditorDialog state={editor} canGlobal={canGlobal} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteTableDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <RecordsDialog state={editor} canGlobal={canGlobal} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ── Create / edit table ──
function TableEditorDialog({ state, canGlobal, onClose }: { state: EditorState; canGlobal: boolean; onClose: () => void }) {
  const { t } = useTranslation("lookups");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isCreate = state.mode === "create";
  const table = state.mode === "edit" ? state.table : undefined;
  const isOpen = isCreate || state.mode === "edit";

  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isManageable, setIsManageable] = useState(true);
  const [visibleInMenu, setVisibleInMenu] = useState(false);
  const [isGlobal, setIsGlobal] = useState(false);
  const [sortOrder, setSortOrder] = useState("0");

  useEffect(() => {
    if (!isOpen) return;
    setCode(table?.code ?? "");
    setName(table?.name ?? "");
    setDescription(table?.description ?? "");
    setIsManageable(table?.isManageable ?? true);
    setVisibleInMenu(table?.visibleInMenu ?? false);
    setIsGlobal(table?.isGlobal ?? false);
    setSortOrder(String(table?.sortOrder ?? 0));
  }, [isOpen, table]);

  const save = useMutation({
    mutationFn: async () => {
      if (isCreate) {
        await createBasicTable({ code: code.trim(), name: name.trim(), description: description.trim() || null, isManageable, sortOrder: Number(sortOrder) || 0, visibleInMenu, isGlobal });
      } else {
        await updateBasicTable({ id: table!.id, name: name.trim(), description: description.trim() || null, isManageable, sortOrder: Number(sortOrder) || 0, visibleInMenu });
      }
    },
    onSuccess: () => { toast.success(isCreate ? tc("feedback.created") : tc("feedback.updated")); queryClient.invalidateQueries({ queryKey: KEY }); onClose(); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const canSubmit = !!name.trim() && (!isCreate || !!code.trim());
  const onSubmit = (e: FormEvent<HTMLFormElement>) => { e.preventDefault(); if (canSubmit) save.mutate(); };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isCreate ? t("actions.create") : t("actions.edit")}</DialogTitle>
            <DialogDescription>{t("createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="bt-code" span={4} label={t("fields.code")} required hint={isCreate ? t("codeHint") : t("codeImmutable")}>
                <Input id="bt-code" value={code} onChange={(e) => setCode(e.target.value)} disabled={!isCreate} className="font-mono" required maxLength={64} />
              </Field>
              <Field id="bt-name" span={8} label={t("fields.name")} required>
                <Input id="bt-name" value={name} onChange={(e) => setName(e.target.value)} autoFocus required maxLength={128} />
              </Field>
              <Field id="bt-desc" span={12} label={t("fields.description")}>
                <Textarea id="bt-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} maxLength={512} />
              </Field>
              <Field id="bt-order" span={4} label={t("fields.sortOrder")}>
                <Input id="bt-order" type="number" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} />
              </Field>
              <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-8">
                <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isManageable} onCheckedChange={setIsManageable} />{t("fields.isManageable")}
                </label>
                <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={visibleInMenu} onCheckedChange={setVisibleInMenu} />{t("fields.visibleInMenu")}
                </label>
                {isCreate && (
                  <label className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                    <Switch checked={isGlobal} onCheckedChange={setIsGlobal} disabled={!canGlobal} />{t("fields.isGlobal")}
                    {!canGlobal && <span className="text-[11px] font-normal text-[var(--color-muted-foreground)]">— {t("globalRootOnly")}</span>}
                  </label>
                )}
              </div>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline" disabled={save.isPending}><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
            <Button type="submit" disabled={save.isPending || !canSubmit}><Check className="size-4" />{save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Delete table ──
function DeleteTableDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("lookups");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const table = state.mode === "delete" ? state.table : undefined;

  const del = useMutation({
    mutationFn: (id: string) => deleteBasicTable(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: KEY }); onClose(); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("actions.delete")}</DialogTitle>
          <DialogDescription>{t("deleteConfirm", { name: table?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline" disabled={del.isPending}><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
          <Button variant="destructive" onClick={() => table && del.mutate(table.id)} disabled={del.isPending}>
            <Trash2 className="size-4" />{del.isPending ? tc("feedback.saving") : t("actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Records master-detail editor ──
type RecRow = BasicRecordInput;

function RecordsDialog({ state, canGlobal, onClose }: { state: EditorState; canGlobal: boolean; onClose: () => void }) {
  const { t } = useTranslation("lookups");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "records";
  const table = state.mode === "records" ? state.table : undefined;
  const tableId = table?.id;
  const readOnly = (table?.isGlobal ?? false) && !canGlobal;

  const detailQ = useQuery({
    queryKey: ["lookups", "tables", "detail", tableId],
    queryFn: () => getBasicTableById(tableId!),
    enabled: isOpen && !!tableId,
  });

  const [rows, setRows] = useState<RecRow[]>([]);
  useEffect(() => {
    if (detailQ.data) setRows(detailQ.data.records.map((r) => ({ id: r.id, code: r.code, value: r.value, sortOrder: r.sortOrder, isActive: r.isActive })));
  }, [detailQ.data]);

  const save = useMutation({
    mutationFn: () => upsertBasicRecords(tableId!, rows.filter((r) => r.code.trim() && r.value.trim())),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: KEY });
      queryClient.invalidateQueries({ queryKey: ["lookups", "tables", "detail", tableId] });
      queryClient.invalidateQueries({ queryKey: ["lookups", "records"] });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  const update = (i: number, patch: Partial<RecRow>) => setRows((p) => p.map((r, idx) => idx === i ? { ...r, ...patch } : r));
  const addRow = () => setRows((p) => [...p, { id: null, code: "", value: "", sortOrder: p.length * 10, isActive: true }]);
  const hasDupes = useMemo(() => {
    const codes = rows.filter((r) => r.code.trim()).map((r) => r.code.trim().toLowerCase());
    return new Set(codes).size !== codes.length;
  }, [rows]);

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{table?.name}</DialogTitle>
          <DialogDescription>{t("recordsDesc")}{readOnly ? ` · ${t("globalReadOnly")}` : ""}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="space-y-2">
            <div className="flex items-center gap-2 px-1 text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
              <span className="w-1/3">{t("fields.recordCode")}</span>
              <span className="flex-1">{t("fields.recordValue")}</span>
              <span className="w-20 text-center">{t("fields.order")}</span>
              <span className="w-16 text-center">{t("fields.active")}</span>
              <span className="w-8" />
            </div>
            {rows.length === 0 && <p className="px-1 text-[12.5px] text-[var(--color-muted-foreground)]">{t("noRecords")}</p>}
            {rows.map((r, i) => (
              <div key={r.id ?? `new-${i}`} className="flex items-center gap-2">
                <Input className="w-1/3 font-mono" value={r.code} disabled={readOnly || !!r.id}
                  onChange={(e) => update(i, { code: e.target.value })} placeholder={t("fields.recordCode")} />
                <Input className="flex-1" value={r.value} disabled={readOnly}
                  onChange={(e) => update(i, { value: e.target.value })} placeholder={t("fields.recordValue")} />
                <Input className="w-20" type="number" value={String(r.sortOrder ?? 0)} disabled={readOnly}
                  onChange={(e) => update(i, { sortOrder: Number(e.target.value) || 0 })} />
                <span className="flex w-16 justify-center">
                  <Switch checked={r.isActive ?? true} disabled={readOnly} onCheckedChange={(v) => update(i, { isActive: v })} />
                </span>
                <button type="button" disabled={readOnly} aria-label={tc("actions.delete")}
                  onClick={() => setRows((p) => p.filter((_, idx) => idx !== i))}
                  className="flex w-8 justify-center text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)] disabled:opacity-40">
                  <Trash2 className="size-4" />
                </button>
              </div>
            ))}
            {!readOnly && (
              <Button type="button" variant="outline" size="sm" onClick={addRow} className="mt-1">
                <Plus className="size-4" />{t("addRecord")}
              </Button>
            )}
            {hasDupes && <p className="text-[12px] text-[var(--color-destructive)]">{t("dupeCodes")}</p>}
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild><Button type="button" variant="outline"><X className="size-4" />{tc("actions.close")}</Button></DialogClose>
          {!readOnly && (
            <Button type="button" onClick={() => save.mutate()} disabled={save.isPending || hasDupes}>
              <Check className="size-4" />{save.isPending ? tc("feedback.saving") : t("saveRecords")}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
