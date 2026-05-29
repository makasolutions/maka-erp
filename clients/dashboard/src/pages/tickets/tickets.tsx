import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ChevronRight,
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
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
  type EntityStatusTone,
} from "@/components/list";
import { MakaGrid } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { cn } from "@/lib/cn";
import { describe, formatRelative } from "@/lib/list-helpers";
import { useUserDisplay } from "@/lib/use-user-display";

// Row shape fed to MakaGrid — base ticket + pre-translated chip labels so the
// Syncfusion cell templates stay hook-free for priority/status.
type TicketRow = TicketDto & {
  priorityLabel: string;
  statusLabel: string;
};

const PAGE_SIZE = 20;

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

const DESKTOP_GRID =
  "grid-cols-[1fr_100px_120px_140px_110px_24px] lg:grid-cols-[1fr_110px_140px_160px_120px_24px]";

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TicketsPage() {
  const { t } = useTranslation("tickets");
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TicketStatus | null>(null);
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  // Debounce search
  useEffect(() => {
    const id = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(id);
  }, [search]);

  useEffect(() => setPageNumber(1), [debouncedSearch, statusFilter, priorityFilter]);

  const query = useQuery({
    queryKey: [
      "tickets",
      "list",
      { search: debouncedSearch, statusFilter, priorityFilter, pageNumber },
    ],
    queryFn: () =>
      searchTickets({
        search: debouncedSearch || undefined,
        status: statusFilter ?? undefined,
        priority: priorityFilter ?? undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  // ── MakaGrid (side-by-side test) — same filters, fetched in bulk so the
  // grid can paginate / sort / group client-side. Coexists with the original.
  const makaQuery = useQuery({
    queryKey: [
      "tickets",
      "maka-list",
      { search: debouncedSearch, statusFilter, priorityFilter },
    ],
    queryFn: () =>
      searchTickets({
        search: debouncedSearch || undefined,
        status: statusFilter ?? undefined,
        priority: priorityFilter ?? undefined,
        pageNumber: 1,
        pageSize: 1000,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
    placeholderData: keepPreviousData,
  });

  const makaRows: TicketRow[] = useMemo(
    () =>
      (makaQuery.data?.items ?? []).map((tk) => ({
        ...tk,
        priorityLabel: t(PRIORITY_KEY[tk.priority]),
        statusLabel: t(STATUS_KEY[tk.status]),
      })),
    [makaQuery.data, t],
  );

  const makaColumns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "title", headerText: t("cols.subject"), template: TicketSubjectCell as any, minWidth: 240, clipMode: "EllipsisWithTooltip" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "priority", headerText: t("cols.priority"), template: TicketPriorityCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "status", headerText: t("cols.status"), template: TicketStatusCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "assignedToUserId", headerText: t("cols.assignee"), template: TicketAssigneeCell as any, width: 180, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "createdAtUtc", headerText: t("cols.created"), template: TicketCreatedCell as any, width: 140 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "reporterUserId", headerText: t("cols.reporter"), template: TicketReporterCell as any, width: 180, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "updatedAtUtc", headerText: t("cols.updated"), template: TicketUpdatedCell as any, width: 140 },
    ],
    [t],
  );

  const filtersApplied = statusFilter !== null || priorityFilter !== null;
  const searchActive = debouncedSearch.length > 0 || filtersApplied;

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
        total={data?.totalCount ?? null}
        unit={t("unit")}
        description={t("description")}
      >
        <Button
          perm={P.tickets.create}
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("newTicket")}
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder={t("searchPlaceholder")}
      />

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<TicketStatus | null>
          label={t("filter.status")}
          value={statusFilter}
          onChange={setStatusFilter}
          options={statusOptions}
        />
        <EntityFilterPill<TicketPriority | null>
          label={t("filter.priority")}
          value={priorityFilter}
          onChange={setPriorityFilter}
          options={priorityOptions}
        />
      </div>

      {/* Results */}
      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_GRID} rows={6} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={TicketIcon}
          title={searchActive ? t("empty.searchTitle") : t("empty.title")}
          body={
            searchActive
              ? debouncedSearch
                ? t("empty.searchBody", { term: debouncedSearch })
                : t("empty.filterBody")
              : t("empty.body")
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => {
                  setSearch("");
                  setStatusFilter(null);
                  setPriorityFilter(null);
                }}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                {t("clearFilters")}
              </Button>
            ) : (
              <Button
                perm={P.tickets.create}
                onClick={() => setEditor({ mode: "create" })}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                {t("openTicket")}
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("found", { count: data?.totalCount ?? 0 })}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((ticket) => (
              <MobileCard key={ticket.id} ticket={ticket} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_GRID}>
              <span>{t("cols.subject")}</span>
              <span>{t("cols.priority")}</span>
              <span>{t("cols.status")}</span>
              <span>{t("cols.assignee")}</span>
              <span>{t("cols.updated")}</span>
              <span />
            </EntityListHeader>
            {items.map((ticket, i) => (
              <DesktopRow
                key={ticket.id}
                ticket={ticket}
                isLast={i === items.length - 1}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? pageNumber}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{describe(query.error)}</span>
        </div>
      )}

      {/* ── MakaGrid view (testing) — coexists with the original list above ── */}
      <section className="space-y-3 border-t border-[var(--color-border)] pt-6">
        <div>
          <h2 className="font-display text-[15px] font-semibold text-[var(--color-foreground)]">
            {t("experimentalGrid")}
          </h2>
          <p className="text-[12.5px] text-[var(--color-muted-foreground)]">
            {t("experimentalGridDesc")}
          </p>
        </div>
        <MakaGrid<TicketRow>
          dataSource={makaRows}
          columns={makaColumns}
          isLoading={makaQuery.isLoading && makaRows.length === 0}
          fileName="tickets"
          permissions={{ create: P.tickets.create }}
          onCreate={() => setEditor({ mode: "create" })}
          onRowClick={(row) => navigate(`/tickets/${row.id}`)}
        />
      </section>

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
//  Mobile card — avatar of the reporter + title/number + status badges
// ───────────────────────────────────────────────────────────────────────

function MobileCard({ ticket }: { ticket: TicketDto }) {
  const { t } = useTranslation("tickets");
  return (
    <Link
      to={`/tickets/${ticket.id}`}
      aria-label={t("openTicket")}
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        "transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)] active:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.6)]",
        "outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.4)]",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={ticket.reporterUserId} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {ticket.title}
              </p>
            </div>
            <div className="mt-0.5 flex items-center gap-1.5">
              <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {ticket.number}
              </code>
            </div>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-2">
        <EntityStatusBadge tone={PRIORITY_TONE[ticket.priority]}>
          {t(PRIORITY_KEY[ticket.priority])}
        </EntityStatusBadge>
        <EntityStatusBadge tone={STATUS_TONE[ticket.status]}>
          {t(STATUS_KEY[ticket.status])}
        </EntityStatusBadge>
        <span className="ml-auto font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {formatRelative(ticket.updatedAtUtc ?? ticket.createdAtUtc)}
        </span>
      </div>
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function DesktopRow({
  ticket,
  isLast,
}: {
  ticket: TicketDto;
  isLast: boolean;
}) {
  const { t } = useTranslation("tickets");
  const assignee = useUserDisplay(ticket.assignedToUserId);
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
      {/* Subject — avatar + title + number */}
      <Link
        to={`/tickets/${ticket.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={ticket.reporterUserId} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {ticket.title}
          </span>
          <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {ticket.number}
          </code>
        </div>
      </Link>

      {/* Priority */}
      <span>
        <EntityStatusBadge tone={PRIORITY_TONE[ticket.priority]}>
          {t(PRIORITY_KEY[ticket.priority])}
        </EntityStatusBadge>
      </span>

      {/* Status */}
      <span>
        <EntityStatusBadge tone={STATUS_TONE[ticket.status]}>
          {t(STATUS_KEY[ticket.status])}
        </EntityStatusBadge>
      </span>

      {/* Assignee */}
      <div className="flex min-w-0 items-center gap-2">
        {ticket.assignedToUserId ? (
          <>
            <EntityInitialsAvatar name={assignee.name} size={24} />
            <span
              title={assignee.handle ?? ticket.assignedToUserId}
              className="truncate text-[12px] text-[var(--color-foreground)]"
            >
              {assignee.name}
            </span>
          </>
        ) : (
          <span className="font-mono text-[11px] uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
            {t("unassigned")}
          </span>
        )}
      </div>

      {/* Updated */}
      <span className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatRelative(ticket.updatedAtUtc ?? ticket.createdAtUtc)}
      </span>

      {/* Trailing chevron */}
      <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
    </EntityListRow>
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
  const { t } = useTranslation("tickets");
  const assignee = useUserDisplay(ticket.assignedToUserId);
  if (!ticket.assignedToUserId) {
    return (
      <span className="font-mono text-[11px] uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
        {t("unassigned")}
      </span>
    );
  }
  return (
    <div className="flex min-w-0 items-center gap-2">
      <EntityInitialsAvatar name={assignee.name} size={22} />
      <span
        title={assignee.handle ?? ticket.assignedToUserId}
        className="truncate text-[12px] text-[var(--color-foreground)]"
      >
        {assignee.name}
      </span>
    </div>
  );
}

function TicketReporterCell(ticket: TicketRow) {
  const reporter = useUserDisplay(ticket.reporterUserId);
  return (
    <div className="flex min-w-0 items-center gap-2">
      <EntityInitialsAvatar name={reporter.name} size={22} />
      <span
        title={reporter.handle ?? ticket.reporterUserId}
        className="truncate text-[12px] text-[var(--color-foreground)]"
      >
        {reporter.name}
      </span>
    </div>
  );
}

function TicketCreatedCell(ticket: TicketRow) {
  return (
    <span className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
      {formatRelative(ticket.createdAtUtc)}
    </span>
  );
}

function TicketUpdatedCell(ticket: TicketRow) {
  return (
    <span className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
      {formatRelative(ticket.updatedAtUtc ?? ticket.createdAtUtc)}
    </span>
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
