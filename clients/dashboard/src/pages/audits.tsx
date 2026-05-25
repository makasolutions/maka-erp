import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertTriangle,
  ChevronRight,
  CircleAlert,
  Database,
  ExternalLink,
  Filter,
  Hash,
  Loader2,
  RefreshCw,
  ScrollText,
  Search,
  Shield,
  ShieldCheck,
  Tag,
  X,
} from "lucide-react";
import {
  AuditEventType,
  AuditSeverity,
  AUDIT_EVENT_TYPE_LABELS,
  AUDIT_SEVERITY_LABELS,
  AUDIT_TAG_LABELS,
  decodeTags,
  getAuditById,
  getAuditsByCorrelation,
  getAuditSummary,
  listAudits,
  type AuditDetailDto,
  type AuditSummaryDto,
} from "@/api/audits";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
  EntityStatusBadge,
} from "@/components/list";
import {
  Dialog,
  DialogClose,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
} from "@/components/ui/dialog";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 25;
const DESKTOP_COLS = "grid-cols-[1.6fr_140px_1.2fr_180px]";

// ────────────────────────────────────────────────────────────────────────
// Time-range presets — keep the SQL window predictable from the UI.
// ────────────────────────────────────────────────────────────────────────

type RangeKey = "24h" | "7d" | "30d" | "90d";
const RANGE_OPTIONS: Array<{ key: RangeKey; label: string; ms: number }> = [
  { key: "24h", label: "24h", ms: 24 * 60 * 60 * 1000 },
  { key: "7d", label: "7d", ms: 7 * 24 * 60 * 60 * 1000 },
  { key: "30d", label: "30d", ms: 30 * 24 * 60 * 60 * 1000 },
  { key: "90d", label: "90d", ms: 90 * 24 * 60 * 60 * 1000 },
];

function rangeBounds(key: RangeKey): { from: string; to: string } {
  const opt = RANGE_OPTIONS.find((r) => r.key === key) ?? RANGE_OPTIONS[1];
  const to = new Date();
  const from = new Date(to.getTime() - opt.ms);
  return { from: from.toISOString(), to: to.toISOString() };
}

// ────────────────────────────────────────────────────────────────────────
// Severity / event-type tone — keep visual language consistent across
// the row colour bar, drawer header, and severity pills.
// ────────────────────────────────────────────────────────────────────────

function severityTone(severity: number): "default" | "info" | "warning" | "danger" {
  if (severity >= AuditSeverity.Critical) return "danger";
  if (severity >= AuditSeverity.Error) return "danger";
  if (severity >= AuditSeverity.Warning) return "warning";
  if (severity >= AuditSeverity.Information) return "info";
  return "default";
}

function severityColorVar(severity: number): string {
  const tone = severityTone(severity);
  return tone === "danger"
    ? "var(--color-destructive)"
    : tone === "warning"
      ? "var(--color-warning)"
      : tone === "info"
        ? "var(--color-info)"
        : "var(--color-muted-foreground)";
}

function eventTypeIcon(eventType: number): React.ComponentType<React.SVGProps<SVGSVGElement>> {
  if (eventType === AuditEventType.Security) return Shield;
  if (eventType === AuditEventType.Exception) return CircleAlert;
  if (eventType === AuditEventType.EntityChange) return Database;
  if (eventType === AuditEventType.Activity) return Activity;
  return Hash;
}

// ────────────────────────────────────────────────────────────────────────
// Time formatting — mono ISO for the table, locale-aware for the drawer.
// ────────────────────────────────────────────────────────────────────────

function fmtIsoDense(iso: string): { date: string; time: string } {
  // UTC — used in the detail drawer where the label explicitly reads "UTC".
  const d = new Date(iso);
  const yyyy = d.getUTCFullYear();
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  const dd = String(d.getUTCDate()).padStart(2, "0");
  const hh = String(d.getUTCHours()).padStart(2, "0");
  const mi = String(d.getUTCMinutes()).padStart(2, "0");
  const ss = String(d.getUTCSeconds()).padStart(2, "0");
  const ms = String(d.getUTCMilliseconds()).padStart(3, "0");
  return { date: `${yyyy}-${mm}-${dd}`, time: `${hh}:${mi}:${ss}.${ms}` };
}

/**
 * Local-timezone timestamp — used in the audit table rows.
 * Temporary until global localization (Grupo 3) is configured.
 */
function fmtLocalDense(iso: string): { date: string; time: string } {
  const d = new Date(iso);
  const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const date = d.toLocaleDateString("es-CO", { timeZone: tz, dateStyle: "short" });
  const time = d.toLocaleTimeString("es-CO", { timeZone: tz, timeStyle: "medium" });
  return { date, time };
}

function fmtRelative(
  iso: string,
  t: (key: string, opts?: Record<string, unknown>) => string,
  now: number = Date.now(),
): string {
  const delta = Math.max(0, Math.floor((now - Date.parse(iso)) / 1000));
  if (delta < 5) return t("relativeTime.justNow");
  if (delta < 60) return t("relativeTime.secondsAgo", { count: delta });
  const m = Math.floor(delta / 60);
  if (m < 60) return t("relativeTime.minutesAgo", { count: m });
  const h = Math.floor(m / 60);
  if (h < 24) return t("relativeTime.hoursAgo", { count: h });
  const days = Math.floor(h / 24);
  return t("relativeTime.daysAgo", { count: days });
}

// ────────────────────────────────────────────────────────────────────────
// Filter state — single source of truth, lifted into URL-synced shape.
// ────────────────────────────────────────────────────────────────────────

type FilterState = {
  range: RangeKey;
  eventType: number | null;
  severity: number | null;
  tagsMask: number;
  source: string;
  user: string;
  correlation: string;
  trace: string;
  search: string;
  entityName: string;
  entityKey: string;
  entityOperation: string;
  page: number;
};

const INITIAL_FILTERS: FilterState = {
  range: "7d",
  eventType: null,
  severity: null,
  tagsMask: 0,
  source: "",
  user: "",
  correlation: "",
  trace: "",
  search: "",
  entityName: "",
  entityKey: "",
  entityOperation: "",
  page: 1,
};

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function AuditsPage() {
  const [filters, setFilters] = useState<FilterState>(INITIAL_FILTERS);
  const [searchInput, setSearchInput] = useState("");
  const [drawerId, setDrawerId] = useState<string | null>(null);
  const { t } = useTranslation("common");

  // Debounce the search input so we don't hammer the API on every keystroke.
  useEffect(() => {
    const t = window.setTimeout(() => {
      setFilters((f) => ({ ...f, search: searchInput, page: 1 }));
    }, 300);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  const window_ = useMemo(() => rangeBounds(filters.range), [filters.range]);

  const auditsQuery = useQuery({
    queryKey: ["audits", "list", filters, window_],
    queryFn: ({ signal }) =>
      listAudits(
        {
          pageNumber: filters.page,
          pageSize: PAGE_SIZE,
          fromUtc: window_.from,
          toUtc: window_.to,
          eventType: (filters.eventType ?? undefined) as AuditEventType | undefined,
          severity: (filters.severity ?? undefined) as AuditSeverity | undefined,
          tags: filters.tagsMask || undefined,
          source: filters.source || undefined,
          userId: filters.user || undefined,
          correlationId: filters.correlation || undefined,
          traceId: filters.trace || undefined,
          search: filters.search || undefined,
          entityName: filters.entityName || undefined,
          entityKey: filters.entityKey || undefined,
          entityOperation: filters.entityOperation || undefined,
        },
        signal,
      ),
    placeholderData: keepPreviousData,
    staleTime: 5_000,
  });

  const summaryQuery = useQuery({
    queryKey: ["audits", "summary", window_],
    queryFn: ({ signal }) =>
      getAuditSummary({ fromUtc: window_.from, toUtc: window_.to }, signal),
    staleTime: 30_000,
  });

  const totals = useMemo(() => {
    const s = summaryQuery.data;
    if (!s) return null;
    const grand = Object.values(s.eventsByType).reduce((a, b) => a + b, 0);
    return {
      grand,
      byType: {
        activity: s.eventsByType[String(AuditEventType.Activity)] ?? 0,
        entity: s.eventsByType[String(AuditEventType.EntityChange)] ?? 0,
        security: s.eventsByType[String(AuditEventType.Security)] ?? 0,
        exception: s.eventsByType[String(AuditEventType.Exception)] ?? 0,
      },
      bySeverity: {
        info: s.eventsBySeverity[String(AuditSeverity.Information)] ?? 0,
        warn: s.eventsBySeverity[String(AuditSeverity.Warning)] ?? 0,
        err: s.eventsBySeverity[String(AuditSeverity.Error)] ?? 0,
        crit: s.eventsBySeverity[String(AuditSeverity.Critical)] ?? 0,
      },
      bySource: Object.entries(s.eventsBySource)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 5),
    };
  }, [summaryQuery.data]);

  const paged = auditsQuery.data;
  const items = paged?.items ?? [];

  const onResetFilters = () => {
    setFilters(INITIAL_FILTERS);
    setSearchInput("");
  };

  const activeChipCount =
    (filters.eventType !== null ? 1 : 0) +
    (filters.severity !== null ? 1 : 0) +
    (filters.tagsMask ? 1 : 0) +
    (filters.source ? 1 : 0) +
    (filters.user ? 1 : 0) +
    (filters.correlation ? 1 : 0) +
    (filters.trace ? 1 : 0) +
    (filters.entityName ? 1 : 0) +
    (filters.entityKey ? 1 : 0) +
    (filters.entityOperation ? 1 : 0);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ScrollText}
        title={t("audits.title")}
        total={paged?.totalCount ?? null}
        unit={t("audits.unit")}
        unitPlural={t("audits.unitPlural")}
        description={t("audits.description")}
      >
        <Button
          variant="outline"
          disabled={auditsQuery.isFetching}
          onClick={() => {
            void auditsQuery.refetch();
            void summaryQuery.refetch();
          }}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <RefreshCw
            className={cn("size-4", auditsQuery.isFetching && "animate-spin")}
          />
          {t("actions.refresh")}
        </Button>
      </EntityPageHeader>

      {/* Filter bar — search lives inside the bar (Row 1) so there's only one input */}
      <FilterBar
        filters={filters}
        searchInput={searchInput}
        activeChipCount={activeChipCount}
        topEntityNames={summaryQuery.data?.topEntityNames ?? []}
        onPatch={(p) => setFilters((f) => ({ ...f, ...p, page: 1 }))}
        onSearchInput={setSearchInput}
        onReset={onResetFilters}
      />

      {/* Summary strip — kept; preserves "window" context */}
      <SummaryStrip
        loading={summaryQuery.isLoading}
        total={totals?.grand ?? 0}
        byType={totals?.byType}
        bySeverity={totals?.bySeverity}
        topSources={totals?.bySource ?? []}
        range={filters.range}
      />

      {/* Results */}
      {auditsQuery.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : auditsQuery.isError ? (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{(auditsQuery.error as Error)?.message ?? t("error.failedLoadAudits")}</span>
        </div>
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={ShieldCheck}
          title={t("audits.empty.title")}
          body={t("audits.empty.body")}
          action={
            activeChipCount > 0 || filters.search ? (
              <Button variant="outline" onClick={onResetFilters} className="h-9 rounded-lg px-4 text-[13px]">
                {t("audits.empty.resetFilters")}
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("audits.eventsFound", { count: paged?.totalCount ?? 0 })}
            </p>
            {auditsQuery.isFetching && (
              <Loader2 className="size-3.5 animate-spin text-[var(--color-muted-foreground)]" />
            )}
          </div>

          {/* Mobile cards */}
          <div className="space-y-2 md:hidden">
            {items.map((row) => (
              <AuditMobileCard
                key={row.id}
                row={row}
                onOpen={() => setDrawerId(row.id)}
              />
            ))}
          </div>

          {/* Desktop list */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>{t("audits.columns.actor")}</span>
              <span>{t("audits.columns.action")}</span>
              <span>{t("audits.columns.entity")}</span>
              <span>{t("audits.columns.timestamp")}</span>
            </EntityListHeader>
            {items.map((row, i) => (
              <AuditDesktopRow
                key={row.id}
                row={row}
                isLast={i === items.length - 1}
                onOpen={() => setDrawerId(row.id)}
              />
            ))}
          </EntityListCard>

          {paged && paged.totalPages > 1 && (
            <EntityPager
              page={filters.page}
              totalPages={paged.totalPages}
              hasPrev={filters.page > 1}
              hasNext={filters.page < paged.totalPages}
              onPrev={() => setFilters((f) => ({ ...f, page: Math.max(1, f.page - 1) }))}
              onNext={() => setFilters((f) => ({ ...f, page: f.page + 1 }))}
            />
          )}
        </div>
      )}

      {/* ── Detail drawer ────────────────────────────────────────────── */}
      <AuditDetailDrawer
        auditId={drawerId}
        onClose={() => setDrawerId(null)}
        onJumpAudit={(id) => setDrawerId(id)}
        onJumpCorrelation={(cid) => {
          setDrawerId(null);
          setFilters((f) => ({ ...INITIAL_FILTERS, range: f.range, correlation: cid }));
        }}
        onJumpTrace={(tid) => {
          setDrawerId(null);
          setFilters((f) => ({ ...INITIAL_FILTERS, range: f.range, trace: tid }));
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card — patient-style: avatar + actor + secondary line + action.
// ───────────────────────────────────────────────────────────────────────

function AuditMobileCard({
  row,
  onOpen,
}: {
  row: AuditSummaryDto;
  onOpen: () => void;
}) {
  const ts = fmtLocalDense(row.occurredAtUtc);
  const Icon = eventTypeIcon(row.eventType);
  const tone = severityTone(row.severity);
  const toneColor = severityColorVar(row.severity);
  const actor = row.userName ?? (row.userId ? `${row.userId.slice(0, 8)}…` : "System");

  return (
    <button
      type="button"
      onClick={onOpen}
      className={cn(
        "block w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        "transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)] active:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.6)]",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={actor} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {actor}
              </p>
              <EntityStatusBadge
                tone={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}
              >
                {AUDIT_SEVERITY_LABELS[row.severity]}
              </EntityStatusBadge>
            </div>
            <div className="mt-0.5 flex items-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
              <Icon className="size-3" style={{ color: toneColor }} aria-hidden />
              <span>{AUDIT_EVENT_TYPE_LABELS[row.eventType]}</span>
            </div>
          </div>
        </div>
        <ChevronRight className="size-4 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-x-3 gap-y-0.5">
        <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {row.source ?? "—"}
        </code>
        <span className="ml-auto font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {ts.time} · {ts.date}
        </span>
      </div>
    </button>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row — Actor | Action | Entity | Timestamp
// ───────────────────────────────────────────────────────────────────────

/** Returns the last dot-segment of a qualified name. "A.B.MyClass" → "MyClass" */
function lastSegment(source: string | null | undefined): string {
  if (!source) return "—";
  const parts = source.split(".");
  return parts[parts.length - 1] ?? source;
}

/** Semantic colour for an entity operation — matches entity-audit-section palette. */
function operationBadgeStyle(op: string | null | undefined): React.CSSProperties {
  switch (op) {
    case "Insert":
      return {
        background: "oklch(from var(--color-success) l c h / 0.12)",
        color: "var(--color-success)",
        borderColor: "oklch(from var(--color-success) l c h / 0.30)",
      };
    case "Update":
      return {
        background: "oklch(from var(--color-info) l c h / 0.12)",
        color: "var(--color-info)",
        borderColor: "oklch(from var(--color-info) l c h / 0.30)",
      };
    case "Delete":
      return {
        background: "oklch(from var(--color-destructive) l c h / 0.12)",
        color: "var(--color-destructive)",
        borderColor: "oklch(from var(--color-destructive) l c h / 0.30)",
      };
    case "SoftDelete":
      return {
        background: "oklch(from var(--color-warning) l c h / 0.12)",
        color: "var(--color-warning)",
        borderColor: "oklch(from var(--color-warning) l c h / 0.30)",
      };
    case "Restore":
      return {
        background: "oklch(from var(--color-accent) l c h / 0.20)",
        color: "var(--color-accent)",
        borderColor: "oklch(from var(--color-accent) l c h / 0.40)",
      };
    default:
      return {
        background: "oklch(from var(--color-muted-foreground) l c h / 0.10)",
        color: "var(--color-muted-foreground)",
        borderColor: "oklch(from var(--color-muted-foreground) l c h / 0.20)",
      };
  }
}

function AuditDesktopRow({
  row,
  isLast,
  onOpen,
}: {
  row: AuditSummaryDto;
  isLast: boolean;
  onOpen: () => void;
}) {
  const { t } = useTranslation("common");
  const ts = fmtLocalDense(row.occurredAtUtc);
  const Icon = eventTypeIcon(row.eventType);
  const tone = severityTone(row.severity);
  const toneColor = severityColorVar(row.severity);
  const actor = row.userName ?? (row.userId ? `${row.userId.slice(0, 8)}…` : "System");
  const tags = decodeTags(row.tags);

  // Entity column: show entity name prominently for EntityChange events.
  const isEntityChange = row.eventType === AuditEventType.EntityChange;
  const primaryEntityLabel = isEntityChange
    ? (row.entityName ?? lastSegment(row.source))
    : lastSegment(row.source);

  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} onClick={onOpen}>
      {/* Actor */}
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={actor} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {actor}
          </div>
          {row.userId && (
            <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {row.userId.slice(0, 8)}…
            </code>
          )}
        </div>
      </div>

      {/* Action (event type + severity) */}
      <div className="flex min-w-0 items-center gap-1.5">
        <Icon className="size-3.5 shrink-0" style={{ color: toneColor }} aria-hidden />
        <div className="min-w-0">
          <div className="truncate text-[12.5px] font-medium tracking-tight">
            {AUDIT_EVENT_TYPE_LABELS[row.eventType]}
          </div>
          <EntityStatusBadge
            tone={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}
          >
            {AUDIT_SEVERITY_LABELS[row.severity]}
          </EntityStatusBadge>
        </div>
      </div>

      {/* Entity — primary name + namespace + operation badge for EntityChange */}
      <div className="min-w-0">
        {/* Primary: short entity name */}
        <div className="flex items-center gap-1.5">
          <span className="truncate text-[12.5px] font-medium text-[var(--color-foreground)]">
            {primaryEntityLabel}
          </span>
          {/* Operation badge — shown only for EntityChange events */}
          {isEntityChange && row.entityOperation && (
            <span
              className="inline-flex shrink-0 items-center rounded-full border px-1.5 py-px font-mono text-[10px] font-semibold leading-none"
              style={operationBadgeStyle(row.entityOperation)}
            >
              {t(`audits.operations.${row.entityOperation.charAt(0).toLowerCase() + row.entityOperation.slice(1)}`)}
            </span>
          )}
        </div>
        {/* Secondary: full namespace or source */}
        <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {row.source ?? "—"}
        </code>
        {/* Tags (kept below namespace) */}
        {tags.length > 0 && (
          <div className="mt-0.5 flex flex-wrap items-center gap-1">
            {tags.slice(0, 2).map((name) => (
              <span
                key={name}
                className="inline-flex items-center gap-0.5 rounded-full bg-[var(--color-muted)] px-1.5 py-0 font-mono text-[10px]"
              >
                <Tag className="size-2.5" />
                {name}
              </span>
            ))}
            {tags.length > 2 && (
              <span className="font-mono text-[10px] text-[var(--color-muted-foreground)]">
                +{tags.length - 2}
              </span>
            )}
          </div>
        )}
      </div>

      {/* Timestamp — local timezone */}
      <div className="flex items-center justify-between gap-2">
        <div className="font-mono text-[11.5px] tabular-nums leading-tight">
          <div className="text-[var(--color-foreground)]">{ts.time}</div>
          <div className="text-[10.5px] text-[var(--color-muted-foreground)]">{ts.date}</div>
        </div>
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Summary strip — compact totals bar above the filters. Shows grand
// total, breakdown by type as a horizontal bar, severity dots, and
// top-5 sources as right-aligned mono chips.
// ────────────────────────────────────────────────────────────────────────

function SummaryStrip({
  loading,
  total,
  byType,
  bySeverity,
  topSources,
  range,
}: {
  loading: boolean;
  total: number;
  byType?: { activity: number; entity: number; security: number; exception: number };
  bySeverity?: { info: number; warn: number; err: number; crit: number };
  topSources: Array<[string, number]>;
  range: RangeKey;
}) {
  const { t } = useTranslation("common");
  if (loading || !byType || !bySeverity) {
    return (
      <Card>
        <CardContent className="grid grid-cols-1 gap-4 px-6 py-4 lg:grid-cols-[auto_1fr_auto] lg:items-center">
          <Skeleton className="h-9 w-24" />
          <Skeleton className="h-3 w-full" />
          <Skeleton className="h-7 w-72" />
        </CardContent>
      </Card>
    );
  }

  // Stack-bar segments — fall back to a single muted segment when the
  // window has zero activity, so the strip still has visual mass.
  const totalSafe = Math.max(1, total);
  const segments = [
    { key: "activity", value: byType.activity, color: "var(--color-primary)" },
    { key: "entity", value: byType.entity, color: "var(--color-info)" },
    { key: "security", value: byType.security, color: "var(--color-success)" },
    { key: "exception", value: byType.exception, color: "var(--color-destructive)" },
  ];

  return (
    <Card className="relative overflow-hidden">
      <CardContent className="grid grid-cols-1 gap-5 px-6 py-4 lg:grid-cols-[auto_1fr_auto] lg:items-center">
        <div>
          <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {t("health.windowLabel", { range })}
          </div>
          <div className="mt-1 font-display text-2xl font-semibold tabular-nums leading-none">
            {new Intl.NumberFormat(undefined).format(total)}
          </div>
          <div className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">{t("health.events")}</div>
        </div>

        <div className="space-y-2">
          <div className="flex h-2 w-full overflow-hidden rounded-full bg-[var(--color-muted)]">
            {segments.map((s) => (
              <div
                key={s.key}
                title={`${s.key}: ${s.value}`}
                style={{
                  width: `${(s.value / totalSafe) * 100}%`,
                  backgroundColor: s.color,
                  transition: "width 600ms var(--ease-out-cubic)",
                }}
              />
            ))}
          </div>
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            <Legend dot="var(--color-primary)" label={t("audits.summary.activity")} value={byType.activity} />
            <Legend dot="var(--color-info)" label={t("audits.summary.entity")} value={byType.entity} />
            <Legend dot="var(--color-success)" label={t("audits.summary.security")} value={byType.security} />
            <Legend dot="var(--color-destructive)" label={t("audits.summary.exception")} value={byType.exception} />
            <span aria-hidden className="ml-auto h-3 w-px bg-[var(--color-border)]" />
            <SeverityDot color="var(--color-warning)" label={t("audits.summary.warn")} value={bySeverity.warn} />
            <SeverityDot color="var(--color-destructive)" label={t("audits.summary.err")} value={bySeverity.err + bySeverity.crit} />
          </div>
        </div>

        <div className="flex flex-col items-end gap-1.5">
          <div className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {t("audits.summary.topSources")}
          </div>
          <div className="flex max-w-[28rem] flex-wrap justify-end gap-1.5">
            {topSources.length === 0 && (
              <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">—</span>
            )}
            {topSources.map(([name, count]) => (
              <span
                key={name}
                className="inline-flex items-center gap-1.5 rounded-full bg-[var(--color-muted)] px-2 py-0.5 font-mono text-[10.5px]"
                title={name}
              >
                <span className="max-w-[12rem] truncate text-[var(--color-foreground)]">{name}</span>
                <span className="tabular-nums text-[var(--color-muted-foreground)]">{count}</span>
              </span>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function Legend({ dot, label, value }: { dot: string; label: string; value: number }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: dot }} />
      <span>{label}</span>
      <span className="tabular-nums text-[var(--color-foreground)]">{value}</span>
    </span>
  );
}

function SeverityDot({ color, label, value }: { color: string; label: string; value: number }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: color }} />
      <span>{label}</span>
      <span className="tabular-nums text-[var(--color-foreground)]">{value}</span>
    </span>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Filter bar — time range presets up top, search + advanced fields below.
// ────────────────────────────────────────────────────────────────────────

function FilterBar({
  filters,
  searchInput,
  activeChipCount,
  topEntityNames,
  onPatch,
  onSearchInput,
  onReset,
}: {
  filters: FilterState;
  searchInput: string;
  activeChipCount: number;
  topEntityNames: string[];
  onPatch: (patch: Partial<FilterState>) => void;
  onSearchInput: (v: string) => void;
  onReset: () => void;
}) {
  const { t } = useTranslation("common");
  const [advancedOpen, setAdvancedOpen] = useState(false);

  return (
    <Card>
      <CardContent className="space-y-3 px-5 py-4">
        {/* Row 1: range presets + search box + advanced toggle */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-1 rounded-lg bg-[var(--color-muted)] p-0.5">
            {RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.key}
                type="button"
                onClick={() => onPatch({ range: opt.key })}
                className={cn(
                  "rounded-md px-3 py-1 font-mono text-[11px] uppercase tracking-[0.08em] transition-colors",
                  filters.range === opt.key
                    ? "bg-[var(--color-card)] text-[var(--color-foreground)] shadow-[0_1px_0_oklch(0_0_0_/_0.05)]"
                    : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>

          <div className="relative flex-1 min-w-[14rem]">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
            <Input
              value={searchInput}
              onChange={(e) => onSearchInput(e.target.value)}
              placeholder={t("audits.searchPlaceholder")}
              className="pl-9"
            />
            {searchInput && (
              <button
                type="button"
                onClick={() => onSearchInput("")}
                aria-label={t("actions.clearSearch")}
                className="absolute right-2 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            )}
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setAdvancedOpen((v) => !v)}
            className="gap-1.5"
          >
            <Filter className="h-3.5 w-3.5" />
            {t("audits.advanced")}
            {activeChipCount > 0 && (
              <Badge variant="brand" className="ml-1 px-1.5 py-0 text-[10px]">
                {activeChipCount}
              </Badge>
            )}
          </Button>

          {activeChipCount > 0 && (
            <button
              type="button"
              onClick={onReset}
              className="font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
            >
              {t("audits.reset")}
            </button>
          )}
        </div>

        {/* Row 2: event type + severity chips */}
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            {t("audits.type")}
          </span>
          {[AuditEventType.Activity, AuditEventType.Security, AuditEventType.EntityChange, AuditEventType.Exception].map((t) => (
            <Chip
              key={t}
              active={filters.eventType === t}
              onClick={() =>
                onPatch({ eventType: filters.eventType === t ? null : t })
              }
            >
              {AUDIT_EVENT_TYPE_LABELS[t]}
            </Chip>
          ))}

          <span aria-hidden className="mx-1 h-4 w-px bg-[var(--color-border)]" />

          <span className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            {t("audits.severity")}
          </span>
          {[AuditSeverity.Information, AuditSeverity.Warning, AuditSeverity.Error, AuditSeverity.Critical].map((s) => (
            <Chip
              key={s}
              active={filters.severity === s}
              tone={severityTone(s)}
              onClick={() =>
                onPatch({ severity: filters.severity === s ? null : s })
              }
            >
              {AUDIT_SEVERITY_LABELS[s]}
            </Chip>
          ))}
        </div>

        {/* Row 3: advanced (collapsible) */}
        {advancedOpen && (
          <div className="grid grid-cols-1 gap-2 border-t border-[var(--color-border)] pt-3 sm:grid-cols-2 lg:grid-cols-3">
            <FieldInput
              label={t("audits.source")}
              placeholder={t("audits.sourcePlaceholder")}
              value={filters.source}
              onChange={(v) => onPatch({ source: v })}
            />
            <FieldInput
              label={t("audits.userId")}
              placeholder={t("audits.userIdPlaceholder")}
              value={filters.user}
              onChange={(v) => onPatch({ user: v })}
            />
            <FieldInput
              label={t("audits.correlation")}
              placeholder={t("audits.correlationPlaceholder")}
              value={filters.correlation}
              onChange={(v) => onPatch({ correlation: v })}
            />
            <FieldInput
              label={t("audits.trace")}
              placeholder={t("audits.tracePlaceholder")}
              value={filters.trace}
              onChange={(v) => onPatch({ trace: v })}
            />
            <div className="sm:col-span-2 lg:col-span-1">
              <div className="mb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                {t("audits.tags")}
              </div>
              <div className="flex flex-wrap gap-1.5">
                {AUDIT_TAG_LABELS.map((tl) => {
                  const active = (filters.tagsMask & tl.flag) !== 0;
                  return (
                    <Chip
                      key={tl.flag}
                      active={active}
                      onClick={() =>
                        onPatch({
                          tagsMask: active
                            ? filters.tagsMask & ~tl.flag
                            : filters.tagsMask | tl.flag,
                        })
                      }
                    >
                      {tl.name}
                    </Chip>
                  );
                })}
              </div>
            </div>

            {/* Entity-change specific filters — only meaningful when eventType=EntityChange
                but available in all contexts so the user can pre-filter before adding that chip */}
            <div className="col-span-full h-px bg-[var(--color-border)]" />

            <FieldSelect
              label={t("audits.entityName")}
              value={filters.entityName}
              onChange={(v) => onPatch({ entityName: v })}
            >
              <option value="">{t("audits.allEntities")}</option>
              {topEntityNames.map((name) => (
                <option key={name} value={name}>{name}</option>
              ))}
            </FieldSelect>

            <FieldSelect
              label={t("audits.entityOperation")}
              value={filters.entityOperation}
              onChange={(v) => onPatch({ entityOperation: v })}
            >
              <option value="">{t("audits.allOperations")}</option>
              <option value="Insert">{t("audits.operations.insert")}</option>
              <option value="Update">{t("audits.operations.update")}</option>
              <option value="Delete">{t("audits.operations.delete")}</option>
              <option value="SoftDelete">{t("audits.operations.softDelete")}</option>
              <option value="Restore">{t("audits.operations.restore")}</option>
            </FieldSelect>

            <FieldInput
              label={t("audits.entityKey")}
              placeholder={t("audits.entityKeyPlaceholder")}
              value={filters.entityKey}
              onChange={(v) => onPatch({ entityKey: v })}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function Chip({
  active,
  tone,
  onClick,
  children,
}: {
  active: boolean;
  tone?: "default" | "info" | "warning" | "danger";
  onClick: () => void;
  children: React.ReactNode;
}) {
  const activeColor =
    tone === "danger"
      ? "var(--color-destructive)"
      : tone === "warning"
        ? "var(--color-warning)"
        : tone === "info"
          ? "var(--color-info)"
          : "var(--color-primary)";
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex h-7 items-center gap-1 rounded-full px-2.5 text-[11px] font-medium tracking-tight",
        "border transition-colors",
        active
          ? "border-transparent text-[var(--color-primary-foreground)] shadow-[var(--highlight-top)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
      )}
      style={active ? { backgroundColor: activeColor } : undefined}
    >
      {children}
    </button>
  );
}

function FieldInput({
  label,
  placeholder,
  value,
  onChange,
}: {
  label: string;
  placeholder: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <label className="block">
      <div className="mb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="h-8 font-mono text-[12px]"
      />
    </label>
  );
}

function FieldSelect({
  label,
  value,
  onChange,
  children,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <div className="mb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={cn(
          "h-8 w-full min-w-0 rounded-lg border border-[var(--color-input)] bg-transparent px-2 py-0",
          "font-mono text-[12px] text-[var(--color-foreground)] shadow-xs outline-none",
          "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
          "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
        )}
      >
        {children}
      </select>
    </label>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Entity-change diff — types and helpers.
// The backend uses SystemTextJsonAuditSerializer with CamelCase policy, so
// payload keys are always camelCase and EntityOperation serialises as a
// string ("Insert", "Update", "Delete", "SoftDelete", "Restore").
// ────────────────────────────────────────────────────────────────────────

type PropertyChangeItem = {
  name: string;
  dataType?: string | null;
  oldValue?: unknown;
  newValue?: unknown;
  isSensitive: boolean;
};

type EntityChangePayload = {
  dbContext: string;
  schema?: string | null;
  table: string;
  entityName: string;
  key: string;
  operation: string;
  changes: PropertyChangeItem[];
  transactionId?: string | null;
};

function parseEntityChangePayload(payload: unknown): EntityChangePayload | null {
  if (!payload || typeof payload !== "object") return null;
  const p = payload as Record<string, unknown>;
  // Guard both casings for safety (serialiser always writes camelCase, but
  // downstream proxies or manual records may preserve PascalCase).
  const changes = p.changes ?? p.Changes;
  if (!Array.isArray(changes)) return null;
  return {
    dbContext: String(p.dbContext ?? p.DbContext ?? ""),
    schema: (p.schema ?? p.Schema) != null ? String(p.schema ?? p.Schema) : null,
    table: String(p.table ?? p.Table ?? ""),
    entityName: String(p.entityName ?? p.EntityName ?? ""),
    key: String(p.key ?? p.Key ?? ""),
    operation: String(p.operation ?? p.Operation ?? ""),
    changes: changes as PropertyChangeItem[],
    transactionId: (p.transactionId ?? p.TransactionId) != null
      ? String(p.transactionId ?? p.TransactionId)
      : null,
  };
}

function formatDiffValue(value: unknown): string {
  if (value === null || value === undefined) return "—";
  if (typeof value === "object") return JSON.stringify(value, null, 2);
  return String(value);
}

// ────────────────────────────────────────────────────────────────────────
// Detail drawer — right-side panel with the full payload, metadata grid,
// and "jump to correlated/traced events" actions. Re-uses the existing
// Dialog primitive but overrides positioning so it slides in from the
// right instead of opening centered.
// ────────────────────────────────────────────────────────────────────────

function AuditDetailDrawer({
  auditId,
  onClose,
  onJumpAudit,
  onJumpCorrelation,
  onJumpTrace,
}: {
  auditId: string | null;
  onClose: () => void;
  onJumpAudit: (id: string) => void;
  onJumpCorrelation: (id: string) => void;
  onJumpTrace: (id: string) => void;
}) {
  const { t } = useTranslation("common");
  const open = auditId !== null;

  const detail = useQuery({
    queryKey: ["audit", "detail", auditId],
    queryFn: ({ signal }) => getAuditById(auditId!, signal),
    enabled: open,
    staleTime: 60_000,
  });

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogPortal>
        <DialogOverlay />
        <DialogPrimitive.Content
          className={cn(
            // Slide-in panel from the right edge.
            "fixed right-0 top-0 z-50 h-screen w-full max-w-[640px] outline-none",
            "bg-card border border-border rounded-none border-y-0 border-r-0 shadow-sm",
            "shadow-[-12px_0_40px_-16px_oklch(0_0_0_/_0.35)]",
            "data-[state=open]:animate-in data-[state=open]:slide-in-from-right",
            "data-[state=closed]:animate-out data-[state=closed]:slide-out-to-right",
            "duration-[var(--duration-default)]",
          )}
        >
          <DialogTitle className="sr-only">{t("audits.drawer.title")}</DialogTitle>
          <DialogDescription className="sr-only">
            {t("audits.drawer.description")}
          </DialogDescription>

          <div className="flex h-full flex-col">
            <DrawerHeader detail={detail.data} loading={detail.isLoading} />

            <div className="flex-1 overflow-y-auto px-6 pb-6">
              {detail.isLoading ? (
                <DrawerSkeleton />
              ) : detail.isError ? (
                <DrawerError message={(detail.error as Error)?.message} />
              ) : detail.data ? (
                <DrawerBody
                  detail={detail.data}
                  onJumpAudit={onJumpAudit}
                  onJumpCorrelation={onJumpCorrelation}
                  onJumpTrace={onJumpTrace}
                />
              ) : null}
            </div>
          </div>

          <DialogClose
            aria-label={t("actions.close")}
            className={cn(
              "absolute right-4 top-4 grid h-8 w-8 place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] transition-colors",
              "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <X className="h-4 w-4" />
          </DialogClose>
        </DialogPrimitive.Content>
      </DialogPortal>
    </Dialog>
  );
}

function DrawerHeader({ detail, loading }: { detail?: AuditDetailDto; loading: boolean }) {
  const { t } = useTranslation("common");
  if (loading || !detail) {
    return (
      <div className="border-b border-[var(--color-border)] px-6 py-5">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="mt-2 h-7 w-48" />
        <Skeleton className="mt-1 h-3 w-72" />
      </div>
    );
  }

  const Icon = eventTypeIcon(detail.eventType);
  const tone = severityTone(detail.severity);
  const toneColor = severityColorVar(detail.severity);
  const ts = fmtIsoDense(detail.occurredAtUtc);
  const tags = decodeTags(detail.tags);

  return (
    <div className="relative border-b border-[var(--color-border)] px-6 py-5">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: `linear-gradient(180deg, oklch(from ${toneColor} l c h / 0.08), transparent 60%)`,
        }}
      />
      <div className="relative">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className="grid h-7 w-7 place-items-center rounded-md ring-1 ring-inset"
            style={{
              background: `linear-gradient(135deg, oklch(from ${toneColor} l c h / 0.20), oklch(from ${toneColor} l c h / 0.02))`,
              color: toneColor,
              boxShadow: `inset 0 0 0 1px oklch(from ${toneColor} l c h / 0.25)`,
            }}
          >
            <Icon className="h-3.5 w-3.5" />
          </span>
          <Badge variant={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}>
            {AUDIT_SEVERITY_LABELS[detail.severity]}
          </Badge>
          <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {AUDIT_EVENT_TYPE_LABELS[detail.eventType]}
          </span>
        </div>
        <div className="mt-2 flex items-baseline gap-3">
          <h2 className="font-display text-xl font-semibold leading-tight tracking-tight">
            {detail.source ?? t("audits.drawer.auditEvent")}
          </h2>
        </div>
        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          <span>{ts.date} {ts.time} UTC</span>
          <span aria-hidden>·</span>
          <span>{fmtRelative(detail.occurredAtUtc, t)}</span>
        </div>
        {tags.length > 0 && (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {tags.map((t) => (
              <span
                key={t}
                className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-2 py-0.5 font-mono text-[10.5px]"
              >
                <Tag className="h-2.5 w-2.5" />
                {t}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function DrawerBody({
  detail,
  onJumpAudit,
  onJumpCorrelation,
  onJumpTrace,
}: {
  detail: AuditDetailDto;
  onJumpAudit: (id: string) => void;
  onJumpCorrelation: (id: string) => void;
  onJumpTrace: (id: string) => void;
}) {
  const { t } = useTranslation("common");
  return (
    <div className="space-y-5 pt-5">
      {/* Identity grid */}
      <section>
        <SectionLabel>{t("audits.drawer.identity")}</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow label={t("audits.drawer.tenant")} value={detail.tenantId ?? "—"} mono />
          <DefRow label={t("audits.drawer.user")} value={detail.userName ?? detail.userId ?? "—"} />
          <DefRow label={t("audits.userId")} value={detail.userId ?? "—"} mono />
          <DefRow label={t("audits.source")} value={detail.source ?? "—"} mono />
        </dl>
      </section>

      {/* Trace grid + jump links */}
      <section>
        <SectionLabel>{t("audits.drawer.trace")}</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow label={t("audits.drawer.traceId")} value={detail.traceId ?? "—"} mono />
          <DefRow label={t("audits.drawer.spanId")} value={detail.spanId ?? "—"} mono />
          <DefRow label={t("audits.drawer.correlationId")} value={detail.correlationId ?? "—"} mono />
          <DefRow label={t("audits.drawer.requestId")} value={detail.requestId ?? "—"} mono />
        </dl>
        <div className="mt-3 flex flex-wrap gap-2">
          {detail.correlationId && (
            <Button
              variant="soft"
              size="sm"
              onClick={() => onJumpCorrelation(detail.correlationId!)}
            >
              <ExternalLink className="mr-1.5 h-3 w-3" /> {t("audits.drawer.allByCorrelation")}
            </Button>
          )}
          {detail.traceId && (
            <Button variant="soft" size="sm" onClick={() => onJumpTrace(detail.traceId!)}>
              <ExternalLink className="mr-1.5 h-3 w-3" /> {t("audits.drawer.allByTrace")}
            </Button>
          )}
        </div>
      </section>

      {/* Related events — every audit sharing this correlation ID,
          rendered as a vertical timeline. Click another row to swap
          the drawer to that audit without closing. */}
      {detail.correlationId && (
        <RelatedEventsSection
          currentId={detail.id}
          correlationId={detail.correlationId}
          currentOccurredAtUtc={detail.occurredAtUtc}
          onJumpAudit={onJumpAudit}
        />
      )}

      {/* Entity-change diff — before/after field table, only for EntityChange events */}
      <EntityChangeDiffSection detail={detail} />

      {/* Payload */}
      <section>
        <div className="flex items-center justify-between">
          <SectionLabel>{t("audits.drawer.payload")}</SectionLabel>
          <CopyButton value={JSON.stringify(detail.payload, null, 2)} />
        </div>
        <pre className="mt-2 max-h-[60vh] overflow-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-1)] p-3 font-mono text-[11px] leading-snug text-[var(--color-foreground)]">
          {JSON.stringify(detail.payload, null, 2)}
        </pre>
      </section>

      {/* Reception window */}
      <section>
        <SectionLabel>{t("audits.drawer.pipeline")}</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow
            label={t("audits.drawer.occurred")}
            value={`${fmtIsoDense(detail.occurredAtUtc).date} ${fmtIsoDense(detail.occurredAtUtc).time}`}
            mono
          />
          <DefRow
            label={t("audits.drawer.received")}
            value={`${fmtIsoDense(detail.receivedAtUtc).date} ${fmtIsoDense(detail.receivedAtUtc).time}`}
            mono
          />
          <DefRow
            label={t("audits.drawer.sinkDelay")}
            value={`${Math.max(0, Date.parse(detail.receivedAtUtc) - Date.parse(detail.occurredAtUtc))} ms`}
            mono
          />
          <DefRow label={t("audits.drawer.auditId")} value={detail.id} mono />
        </dl>
      </section>
    </div>
  );
}

function DrawerSkeleton() {
  return (
    <div className="space-y-5 pt-5">
      {[0, 1, 2].map((i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-3 w-24" />
          <div className="grid grid-cols-2 gap-2">
            {[0, 1, 2, 3].map((j) => (
              <Skeleton key={j} className="h-4 w-full" />
            ))}
          </div>
        </div>
      ))}
      <Skeleton className="h-48 w-full rounded-lg" />
    </div>
  );
}

function DrawerError({ message }: { message?: string }) {
  const { t } = useTranslation("common");
  return (
    <div className="flex flex-col items-center gap-2 pt-12 text-center">
      <AlertTriangle className="h-5 w-5 text-[var(--color-destructive)]" />
      <div className="text-sm font-medium tracking-tight">{t("error.couldNotLoadAudit")}</div>
      <p className="max-w-md text-xs leading-relaxed text-[var(--color-muted-foreground)]">
        {message ?? t("error.couldNotLoadAuditDesc")}
      </p>
    </div>
  );
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
      {children}
    </div>
  );
}

function DefRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="font-mono text-[10px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd className={cn("text-[12.5px]", mono && "font-mono break-all")}>{value}</dd>
    </div>
  );
}

function RelatedEventsSection({
  currentId,
  correlationId,
  currentOccurredAtUtc,
  onJumpAudit,
}: {
  currentId: string;
  correlationId: string;
  currentOccurredAtUtc: string;
  onJumpAudit: (id: string) => void;
}) {
  const { t } = useTranslation("common");
  const related = useQuery({
    queryKey: ["audit", "by-correlation", correlationId],
    queryFn: ({ signal }) => getAuditsByCorrelation(correlationId, {}, signal),
    enabled: !!correlationId,
    staleTime: 30_000,
  });

  // Newest → oldest, capped at 12 to keep the timeline bounded for
  // chatty correlations. Memoised against the underlying response so a
  // re-render doesn't re-sort.
  const sorted = useMemo(() => {
    const items = related.data ?? [];
    return [...items]
      .sort((a, b) => Date.parse(b.occurredAtUtc) - Date.parse(a.occurredAtUtc))
      .slice(0, 12);
  }, [related.data]);
  const others = sorted.filter((r) => r.id !== currentId);
  const currentMs = Date.parse(currentOccurredAtUtc);

  return (
    <section>
      <div className="flex items-baseline justify-between">
        <SectionLabel>{t("audits.drawer.relatedEvents")}</SectionLabel>
        {!related.isLoading && (
          <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {t("audits.drawer.onCorrelation", { count: sorted.length })}
          </span>
        )}
      </div>

      {related.isLoading ? (
        <div className="mt-2 space-y-2">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-10 w-full rounded-md" />
          ))}
        </div>
      ) : others.length === 0 ? (
        <p className="mt-2 text-[11.5px] text-[var(--color-muted-foreground)]">
          {t("audits.drawer.noOtherEvents")}
        </p>
      ) : (
        <ol className="mt-2 relative pl-4">
          {/* Vertical rail — fades top + bottom so it reads as a slice
              of a longer timeline rather than a hard-bounded list. */}
          <span
            aria-hidden
            className="absolute left-1.5 top-1 bottom-1 w-px bg-gradient-to-b from-transparent via-[var(--color-border)] to-transparent"
          />
          {sorted.map((row) => {
            const isCurrent = row.id === currentId;
            const tone = severityColorVar(row.severity);
            const RowIcon = eventTypeIcon(row.eventType);
            const deltaSec = Math.round((Date.parse(row.occurredAtUtc) - currentMs) / 1000);
            const deltaLabel =
              isCurrent
                ? t("audits.drawer.thisEvent")
                : deltaSec === 0
                  ? "0s"
                  : deltaSec > 0
                    ? `+${deltaSec}s`
                    : `${deltaSec}s`;
            return (
              <li key={row.id} className="relative pl-4 pb-2 last:pb-0">
                {/* Node dot */}
                <span
                  aria-hidden
                  className={cn(
                    "absolute -left-0 top-2 h-2.5 w-2.5 rounded-full ring-2 ring-[var(--color-card)]",
                    isCurrent && "shadow-[0_0_0_3px_oklch(from_var(--color-primary)_l_c_h_/_0.18)]",
                  )}
                  style={{ background: isCurrent ? "var(--color-primary)" : tone }}
                />
                <button
                  type="button"
                  onClick={() => !isCurrent && onJumpAudit(row.id)}
                  disabled={isCurrent}
                  className={cn(
                    "group/related flex w-full items-center gap-3 rounded-md px-2 py-1.5 text-left transition-colors",
                    isCurrent
                      ? "bg-[var(--color-primary-soft)] cursor-default"
                      : "hover:bg-[var(--color-accent)] cursor-pointer",
                  )}
                >
                  <RowIcon className="h-3.5 w-3.5 shrink-0" style={{ color: tone }} aria-hidden />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-baseline gap-2">
                      <span className={cn("truncate text-[12px] font-medium tracking-tight", isCurrent && "text-[var(--color-primary)]")}>
                        {row.source ?? AUDIT_EVENT_TYPE_LABELS[row.eventType]}
                      </span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
                        {AUDIT_SEVERITY_LABELS[row.severity]}
                      </span>
                    </span>
                    <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
                      {fmtIsoDense(row.occurredAtUtc).time} · {deltaLabel}
                    </span>
                  </span>
                  {!isCurrent && (
                    <ChevronRight className="h-3 w-3 text-[var(--color-muted-foreground)] transition-transform group-hover/related:translate-x-0.5" />
                  )}
                </button>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}

function CopyButton({ value }: { value: string }) {
  const { t } = useTranslation("common");
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      onClick={async () => {
        try {
          await navigator.clipboard.writeText(value);
          setCopied(true);
          window.setTimeout(() => setCopied(false), 1200);
        } catch {
          /* clipboard unavailable */
        }
      }}
    >
      {copied ? t("audits.drawer.copied") : t("audits.drawer.copy")}
    </button>
  );
}

// ────────────────────────────────────────────────────────────────────────
// EntityChangeDiffSection — before/after field table rendered inside the
// detail drawer when the event type is EntityChange. Shows a colour-coded
// row per changed property: green = created field, red = deleted field,
// yellow = modified field. Sensitive values are masked as ••••••.
// Only rendered when parseEntityChangePayload succeeds (i.e. the payload
// structure matches the EntityChangeEventPayload contract).
// ────────────────────────────────────────────────────────────────────────

function EntityChangeDiffSection({ detail }: { detail: AuditDetailDto }) {
  const { t } = useTranslation("settings");

  if (detail.eventType !== AuditEventType.EntityChange) return null;

  const parsed = parseEntityChangePayload(detail.payload);
  if (!parsed) return null;

  const op = parsed.operation; // "Insert" | "Update" | "Delete" | "SoftDelete" | "Restore"
  const isInsert = op === "Insert";
  const isDelete = op === "Delete" || op === "SoftDelete";

  const opLabel = isInsert
    ? t("audits.diff.created")
    : isDelete
      ? t("audits.diff.deleted")
      : t("audits.diff.updated");

  // Per-row background — uses relative oklch so the tint tracks the
  // current theme's chroma without baking in a fixed lightness.
  function rowBg(change: PropertyChangeItem): string {
    const hasOld = change.oldValue !== null && change.oldValue !== undefined;
    const hasNew = change.newValue !== null && change.newValue !== undefined;
    if (isInsert || (!hasOld && hasNew))
      return "oklch(from var(--color-success) l c h / 0.10)";
    if (isDelete || (hasOld && !hasNew))
      return "oklch(from var(--color-destructive) l c h / 0.08)";
    return "oklch(from var(--color-warning) l c h / 0.07)";
  }

  const isComplex = (v: unknown) =>
    v !== null && v !== undefined && typeof v === "object";

  return (
    <section>
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <SectionLabel>{opLabel}</SectionLabel>
        <span className="font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          {parsed.entityName}
          {parsed.key && (
            <>
              {" · "}
              <span className="opacity-70">{t("audits.diff.key")}: </span>
              {parsed.key}
            </>
          )}
        </span>
      </div>

      {/* Table + entity metadata */}
      <div className="mt-1.5 mb-2 flex flex-wrap gap-x-4 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
        <span>
          <span className="opacity-60">{t("audits.diff.table")}: </span>
          {parsed.table}
        </span>
        <span>
          <span className="opacity-60">{t("audits.diff.entity")}: </span>
          {parsed.entityName}
        </span>
      </div>

      {parsed.changes.length === 0 ? (
        <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
          {t("audits.diff.noChanges")}
        </p>
      ) : (
        <div className="overflow-hidden rounded-lg border border-[var(--color-border)]">
          <table className="w-full text-[12px]">
            <thead>
              <tr className="border-b border-[var(--color-border)] bg-[var(--color-muted)]">
                <th className="px-3 py-1.5 text-left font-mono text-[10px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                  {t("audits.diff.field")}
                </th>
                {!isInsert && (
                  <th className="px-3 py-1.5 text-left font-mono text-[10px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                    {t("audits.diff.before")}
                  </th>
                )}
                {!isDelete && (
                  <th className="px-3 py-1.5 text-left font-mono text-[10px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                    {t("audits.diff.after")}
                  </th>
                )}
              </tr>
            </thead>
            <tbody>
              {parsed.changes.map((change, idx) => (
                <tr
                  key={idx}
                  style={{ backgroundColor: rowBg(change) }}
                  className="border-b border-[var(--color-border)] last:border-0"
                >
                  {/* Field name + optional data type hint */}
                  <td className="px-3 py-2 align-top font-mono text-[11.5px] font-medium text-[var(--color-foreground)]">
                    {change.name}
                    {change.dataType && (
                      <span className="ml-1.5 font-normal text-[10px] text-[var(--color-muted-foreground)]">
                        {change.dataType}
                      </span>
                    )}
                  </td>

                  {/* Before */}
                  {!isInsert && (
                    <td className="px-3 py-2 align-top font-mono text-[11.5px] text-[var(--color-muted-foreground)]">
                      {change.isSensitive ? (
                        <span className="tracking-[0.3em]">{t("audits.diff.sensitive")}</span>
                      ) : isComplex(change.oldValue) ? (
                        <pre className="max-h-24 overflow-auto whitespace-pre-wrap text-[10.5px] leading-relaxed">
                          {formatDiffValue(change.oldValue)}
                        </pre>
                      ) : (
                        <span className="break-all">{formatDiffValue(change.oldValue)}</span>
                      )}
                    </td>
                  )}

                  {/* After */}
                  {!isDelete && (
                    <td className="px-3 py-2 align-top font-mono text-[11.5px] text-[var(--color-foreground)]">
                      {change.isSensitive ? (
                        <span className="tracking-[0.3em] text-[var(--color-muted-foreground)]">
                          {t("audits.diff.sensitive")}
                        </span>
                      ) : isComplex(change.newValue) ? (
                        <pre className="max-h-24 overflow-auto whitespace-pre-wrap text-[10.5px] leading-relaxed">
                          {formatDiffValue(change.newValue)}
                        </pre>
                      ) : (
                        <span className="break-all">{formatDiffValue(change.newValue)}</span>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
