import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useNavigate } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  Eye,
  Plus,
  Ticket as TicketIcon,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  createTicket,
  searchTickets,
  TICKET_PRIORITIES,
  TICKET_STATUSES,
  type CreateTicketInput,
  type TicketDto,
  type TicketPriority,
  type TicketStatus,
} from "@/api/tickets";
import { Button } from "@/components/ui/button";
import { P } from "@/auth/permissions";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import {
  Combobox,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
  Field,
  type EntityStatusTone,
} from "@/components/list";
import { MakaGrid, MakaDateRangePicker } from "@/components/maka";
import type { MakaDateRange } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { getUserById } from "@/api/identity";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

// Row shape fed to MakaGrid — base ticket + pre-resolved label/name fields so
// the Syncfusion cell templates stay hook-free and the Excel filters list
// human-readable text (chip labels, user names) instead of raw enums / GUIDs.
type TicketRow = TicketDto & {
  priorityLabel: string;
  statusLabel: string;
  assigneeName: string;
  reporterName: string;
  /** Date objects so the grid's date columns format + filter correctly. */
  createdAt: Date;
  updatedAt: Date;
};

type EditorState = { mode: "closed" } | { mode: "create" };

// ─── Tone tables — no strings, safe at module level ──────────────────────

const STATUS_TONE: Record<TicketStatus, EntityStatusTone> = {
  Open: "info",
  InProgress: "warning",
  Resolved: "success",
  Closed: "default",
};

const PRIORITY_TONE: Record<TicketPriority, EntityStatusTone> = {
  Low: "default",
  Medium: "info",
  High: "warning",
  Critical: "danger",
};

// ─── Locale key maps — translated by each component via t() ──────────────

const STATUS_KEY: Record<TicketStatus, string> = {
  Open: "status.open",
  InProgress: "status.inProgress",
  Resolved: "status.resolved",
  Closed: "status.closed",
};

const PRIORITY_KEY: Record<TicketPriority, string> = {
  Low: "priority.low",
  Medium: "priority.medium",
  High: "priority.high",
  Critical: "priority.critical",
};

// ─── Grid template — used by header, rows, and the loading skeleton ──────

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TicketsPage() {
  const { t } = useTranslation("tickets");
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [statusFilter, setStatusFilter] = useState<TicketStatus | null>(null);
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  // Collapsible filters/KPIs panel + which tab is active.
  const [panelOpen, setPanelOpen] = useState(true);
  const [activeTab, setActiveTab] = useState<"filters" | "kpis">("filters");

  // ── Data — fetched in bulk (same general filters) so MakaGrid can
  // paginate / sort / group / filter client-side over the full set.
  const makaQuery = useQuery({
    queryKey: ["tickets", "maka-list", { statusFilter, priorityFilter }],
    queryFn: () =>
      searchTickets({
        status: statusFilter ?? undefined,
        priority: priorityFilter ?? undefined,
        pageNumber: 1,
        pageSize: 1000,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
    placeholderData: keepPreviousData,
  });

  // Resolve display names for exactly the users referenced by these tickets,
  // via getUserById (same call the original list uses, react-query–cached).
  const neededUserIds = useMemo(() => {
    const ids = new Set<string>();
    for (const tk of makaQuery.data?.items ?? []) {
      if (tk.assignedToUserId) ids.add(tk.assignedToUserId);
      if (tk.reporterUserId) ids.add(tk.reporterUserId);
    }
    return [...ids].sort();
  }, [makaQuery.data]);

  const namesQuery = useQuery({
    queryKey: ["identity", "ticket-user-names", neededUserIds],
    enabled: neededUserIds.length > 0,
    staleTime: 5 * 60_000,
    queryFn: async () => {
      const entries = await Promise.all(
        neededUserIds.map(async (id) => {
          try {
            const u = await getUserById(id);
            const full = [u.firstName, u.lastName].filter(Boolean).join(" ").trim();
            return [id, full || u.userName || u.email || id.slice(0, 8)] as const;
          } catch {
            return [id, id.slice(0, 8)] as const;
          }
        }),
      );
      return new Map(entries);
    },
  });
  const userNameById = useMemo(
    () => namesQuery.data ?? new Map<string, string>(),
    [namesQuery.data],
  );

  // Date-range filters (page-specific) — applied client-side over the bulk set.
  const [createdRange, setCreatedRange] = useState<MakaDateRange | null>(null);
  const [updatedRange, setUpdatedRange] = useState<MakaDateRange | null>(null);

  const makaRows: TicketRow[] = useMemo(() => {
    const inRange = (iso: string | null | undefined, range: MakaDateRange | null) => {
      if (!range || !iso) return true;
      const d = new Date(iso).getTime();
      const start = range.start.getTime();
      const end = range.end.getTime() + 86_399_999; // include the whole end day
      return d >= start && d <= end;
    };
    return (makaQuery.data?.items ?? [])
      .map((tk) => ({
        ...tk,
        priorityLabel: t(PRIORITY_KEY[tk.priority]),
        statusLabel: t(STATUS_KEY[tk.status]),
        assigneeName: tk.assignedToUserId
          ? userNameById.get(tk.assignedToUserId) ?? tk.assignedToUserId.slice(0, 8)
          : t("unassigned"),
        reporterName: userNameById.get(tk.reporterUserId) ?? tk.reporterUserId.slice(0, 8),
        createdAt: new Date(tk.createdAtUtc),
        updatedAt: new Date(tk.updatedAtUtc ?? tk.createdAtUtc),
      }))
      .filter(
        (r) =>
          inRange(r.createdAtUtc, createdRange) &&
          inRange(r.updatedAtUtc ?? r.createdAtUtc, updatedRange),
      );
  }, [makaQuery.data, t, userNameById, createdRange, updatedRange]);

  // KPIs — reflect the same (date-range) filtered set the grid shows.
  const kpis = useMemo(() => {
    const byStatus: Record<TicketStatus, number> = {
      Open: 0,
      InProgress: 0,
      Resolved: 0,
      Closed: 0,
    };
    for (const r of makaRows) byStatus[r.status] = (byStatus[r.status] ?? 0) + 1;
    return { total: makaRows.length, byStatus };
  }, [makaRows]);

  const makaColumns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "title", headerText: t("cols.subject"), template: TicketSubjectCell as any, minWidth: 240, clipMode: "EllipsisWithTooltip" },
      // field = priorityLabel so filter + grouping show translated labels
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "priorityLabel", headerText: t("cols.priority"), template: TicketPriorityCell as any, width: 130 },
      // field = statusLabel so filter + grouping show translated labels
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "statusLabel", headerText: t("cols.status"), template: TicketStatusCell as any, width: 130 },
      // field = assigneeName so the Excel filter lists names, not GUIDs
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "assigneeName", headerText: t("cols.assignee"), template: TicketAssigneeCell as any, width: 180 },
      // Date column — shows the formatted date (no template) and filters as a date
      { field: "createdAt", headerText: t("cols.created"), width: 150, type: "date" },
      // field = reporterName so the Excel filter lists names, not GUIDs
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "reporterName", headerText: t("cols.reporter"), template: TicketReporterCell as any, width: 180 },
      // Date column — shows the formatted date (no template) and filters as a date
      { field: "updatedAt", headerText: t("cols.updated"), width: 150, type: "date" },
    ],
    [t],
  );

  // Built inside component so labels react to language change
  const statusOptions = useMemo(
    () => [
      { value: null as TicketStatus | null, label: t("filter.allStatuses") },
      ...TICKET_STATUSES.map((s) => ({ value: s as TicketStatus | null, label: t(STATUS_KEY[s]) })),
    ],
    [t],
  );

  const priorityOptions = useMemo(
    () => [
      { value: null as TicketPriority | null, label: t("filter.anyPriority") },
      ...TICKET_PRIORITIES.map((p) => ({ value: p as TicketPriority | null, label: t(PRIORITY_KEY[p]) })),
    ],
    [t],
  );

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={TicketIcon}
        title={t("title")}
        total={makaQuery.data?.totalCount ?? null}
        unit={t("unit")}
        description={t("description")}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {t("actionsPanel")}
        </Button>
        <Button
          perm={P.tickets.create}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("newTicket")}
        </Button>
      </EntityPageHeader>

      {/* Collapsible panel — Tab 1: filters · Tab 2: KPIs */}
      {panelOpen && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          {/* Tab strip */}
          <div className="flex items-center gap-1 border-b border-[var(--color-border)] px-3 pt-2">
            {(["filters", "kpis"] as const).map((tab) => (
              <button
                key={tab}
                type="button"
                onClick={() => setActiveTab(tab)}
                className={cn(
                  "relative -mb-px rounded-t-md px-3.5 py-2 text-[13px] font-medium transition-colors",
                  activeTab === tab
                    ? "border-b-2 border-[var(--color-primary)] text-[var(--color-foreground)]"
                    : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
                )}
              >
                {tab === "filters" ? t("tabs.filters") : t("tabs.kpis")}
              </button>
            ))}
          </div>

          {/* Tab 1 — basic filters: Created · Updated · Status · Priority
              (labels match the grid column headers) */}
          {activeTab === "filters" && (
            <div className="flex flex-wrap items-start gap-x-6 gap-y-4 p-4">
              <MakaDateRangePicker
                label={t("cols.created")}
                value={createdRange}
                onChange={setCreatedRange}
              />
              <MakaDateRangePicker
                label={t("cols.updated")}
                value={updatedRange}
                onChange={setUpdatedRange}
              />
              <div className="flex flex-col gap-1.5">
                <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("cols.status")}
                </span>
                <EntityFilterPill<TicketStatus | null>
                  label={t("cols.status")}
                  value={statusFilter}
                  onChange={setStatusFilter}
                  options={statusOptions}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("cols.priority")}
                </span>
                <EntityFilterPill<TicketPriority | null>
                  label={t("cols.priority")}
                  value={priorityFilter}
                  onChange={setPriorityFilter}
                  options={priorityOptions}
                />
              </div>
            </div>
          )}

          {/* Tab 2 — KPI dashboard (reflects the date-range filters) */}
          {activeTab === "kpis" && (
            <div className="grid grid-cols-2 gap-3 p-4 sm:grid-cols-3 lg:grid-cols-5">
              <KpiCard label={t("kpi.total")} value={kpis.total} tone="default" />
              <KpiCard label={t("status.open")} value={kpis.byStatus.Open} tone="info" />
              <KpiCard label={t("status.inProgress")} value={kpis.byStatus.InProgress} tone="warning" />
              <KpiCard label={t("status.resolved")} value={kpis.byStatus.Resolved} tone="success" />
              <KpiCard label={t("status.closed")} value={kpis.byStatus.Closed} tone="default" />
            </div>
          )}
        </div>
      )}

      <MakaGrid<TicketRow>
        dataSource={makaRows}
        columns={makaColumns}
        isLoading={makaQuery.isLoading && makaRows.length === 0}
        fileName="tickets"
        entityName={t("unit")}
        permissions={{ create: P.tickets.create }}
        onCreate={() => setEditor({ mode: "create" })}
        onRowClick={(row) => navigate(`/tickets/${row.id}`)}
        onClearFilters={() => {
          setStatusFilter(null);
          setPriorityFilter(null);
          setCreatedRange(null);
          setUpdatedRange(null);
        }}
      />

      {makaQuery.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{describe(makaQuery.error)}</span>
        </div>
      )}

      <CreateTicketDialog
        open={editor.mode === "create"}
        onClose={() => setEditor({ mode: "closed" })}
        onCreated={() => {
          void queryClient.invalidateQueries({ queryKey: ["tickets"] });
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  KPI card — small metric tile for the dashboard tab
// ───────────────────────────────────────────────────────────────────────

function KpiCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "default" | "info" | "warning" | "success";
}) {
  const accent =
    tone === "info" ? "var(--color-info)"
    : tone === "warning" ? "var(--color-warning)"
    : tone === "success" ? "var(--color-success)"
    : "var(--color-primary)";
  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-4 py-3">
      <div className="flex items-center gap-2">
        <span aria-hidden className="size-2 rounded-full" style={{ backgroundColor: accent }} />
        <span className="truncate text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {label}
        </span>
      </div>
      <div className="mt-1 font-display text-[26px] font-semibold leading-none tabular-nums text-[var(--color-foreground)]">
        {value}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  MakaGrid cell templates — rendered by Syncfusion per row. Each receives
//  the row object as its props. Priority/Status read the pre-translated
//  labels on TicketRow (hook-free); Assignee/Reporter resolve display names
//  via useUserDisplay (hooks are supported in EJ2 React templates).
// ───────────────────────────────────────────────────────────────────────

function TicketSubjectCell(ticket: TicketRow) {
  return (
    <div className="flex min-w-0 items-center gap-3">
      <EntityInitialsAvatar name={ticket.reporterUserId} size={32} />
      <div className="min-w-0">
        <span className="block truncate text-[13px] font-medium text-[var(--color-foreground)]">
          {ticket.title}
        </span>
        <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {ticket.number}
        </code>
      </div>
    </div>
  );
}

function TicketPriorityCell(ticket: TicketRow) {
  return (
    <EntityStatusBadge tone={PRIORITY_TONE[ticket.priority]}>
      {ticket.priorityLabel}
    </EntityStatusBadge>
  );
}

function TicketStatusCell(ticket: TicketRow) {
  return (
    <EntityStatusBadge tone={STATUS_TONE[ticket.status]}>
      {ticket.statusLabel}
    </EntityStatusBadge>
  );
}

function TicketAssigneeCell(ticket: TicketRow) {
  if (!ticket.assignedToUserId) {
    return (
      <span className="font-mono text-[11px] uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
        {ticket.assigneeName}
      </span>
    );
  }
  return (
    <div className="flex min-w-0 items-center gap-2">
      <EntityInitialsAvatar name={ticket.assigneeName} size={22} />
      <span
        title={ticket.assignedToUserId}
        className="truncate text-[12px] text-[var(--color-foreground)]"
      >
        {ticket.assigneeName}
      </span>
    </div>
  );
}

function TicketReporterCell(ticket: TicketRow) {
  return (
    <div className="flex min-w-0 items-center gap-2">
      <EntityInitialsAvatar name={ticket.reporterName} size={22} />
      <span
        title={ticket.reporterUserId}
        className="truncate text-[12px] text-[var(--color-foreground)]"
      >
        {ticket.reporterName}
      </span>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateTicketDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: () => void;
}) {
  const { t } = useTranslation("tickets");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<TicketPriority>("Medium");

  useEffect(() => {
    if (open) {
      setTitle("");
      setDescription("");
      setPriority("Medium");
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateTicketInput) => createTicket(input),
    onSuccess: () => {
      toast.success(t("toast.opened"));
      onCreated();
      onClose();
    },
    onError: (err: unknown) => {
      toast.error(describe(err));
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      title: title.trim(),
      description: description.trim() || null,
      priority,
    });
  };

  const priorityOptions = useMemo(
    () =>
      TICKET_PRIORITIES.map((p) => ({
        value: p,
        label: t(PRIORITY_KEY[p]),
      })),
    [t],
  );

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <TicketIcon className="size-4 text-[var(--color-primary)]" />
              {t("dialog.openTitle")}
            </DialogTitle>
            <DialogDescription>
              {t("dialog.openDesc")}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="ticket-title" label={t("dialog.titleLabel")} required>
              <Input
                id="ticket-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={t("dialog.titlePlaceholder")}
                maxLength={160}
                autoFocus
                required
              />
            </Field>
            <Field id="ticket-description" label={t("dialog.descLabel")}>
              <textarea
                id="ticket-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("dialog.descPlaceholder")}
                rows={4}
                className={cn(
                  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                )}
                maxLength={4096}
              />
            </Field>
            <Field id="ticket-priority" label={t("dialog.priorityLabel")}>
              <Combobox
                id="ticket-priority"
                variant="field"
                label={t("dialog.priorityLabel")}
                value={priority}
                onChange={(v) => setPriority((v as TicketPriority) ?? "Medium")}
                options={priorityOptions}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={!title.trim() || mutation.isPending}>
              {mutation.isPending ? t("dialog.opening") : t("openTicket")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
