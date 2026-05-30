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
  Eye,
  Hash,
  ScrollText,
  Shield,
  Tag,
  X,
} from "lucide-react";
import {
  AuditEventType,
  AuditSeverity,
  AuditTag,
  AUDIT_TAG_LABELS,
  getAuditById,
  getAuditsByCorrelation,
  getAuditSummary,
  listAudits,
  type AuditDetailDto,
  type AuditSummaryDto,
} from "@/api/audits";
import { searchUsers } from "@/api/identity";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Combobox,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
import {
  MakaGridServer,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
  MakaDateRangePicker,
  makaPresetRange,
} from "@/components/maka";
import type { MakaDateRange } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
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
import { useLocalization } from "@/contexts/localization-context";

// Filter dropdowns: same surface/size as the rest of the filter row (card bg,
// hairline border, accent hover, h-8) — mirrors the catalog filter combos.
const AUDIT_FILTER_COMBO =
  "h-8 w-52 rounded-md border-[var(--color-border)] bg-[var(--color-card)] shadow-none " +
  "hover:border-[var(--color-border)] hover:bg-[var(--color-accent)]";

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
// i18n label helpers — each accepts t from useTranslation("common") so
// labels update when the UI language changes instead of being hardcoded.
// ────────────────────────────────────────────────────────────────────────

type TranslateFn = (key: string) => string;

function fmtEventType(t: TranslateFn, eventType: number): string {
  switch (eventType) {
    case AuditEventType.EntityChange: return t("audits.eventTypes.entity");
    case AuditEventType.Security:     return t("audits.eventTypes.security");
    case AuditEventType.Activity:     return t("audits.eventTypes.activity");
    case AuditEventType.Exception:    return t("audits.eventTypes.exception");
    default:                          return t("audits.eventTypes.unknown");
  }
}

function fmtSeverity(t: TranslateFn, severity: number): string {
  switch (severity) {
    case AuditSeverity.Trace:       return t("audits.severities.trace");
    case AuditSeverity.Debug:       return t("audits.severities.debug");
    case AuditSeverity.Information: return t("audits.severities.information");
    case AuditSeverity.Warning:     return t("audits.severities.warning");
    case AuditSeverity.Error:       return t("audits.severities.error");
    case AuditSeverity.Critical:    return t("audits.severities.critical");
    default:                        return "—";
  }
}

const TAG_I18N_KEY: Partial<Record<number, string>> = {
  [AuditTag.PiiMasked]:      "audits.tagLabels.piiMasked",
  [AuditTag.OutOfQuota]:     "audits.tagLabels.outOfQuota",
  [AuditTag.Sampled]:        "audits.tagLabels.sampled",
  [AuditTag.RetainedLong]:   "audits.tagLabels.retainedLong",
  [AuditTag.HealthCheck]:    "audits.tagLabels.healthCheck",
  [AuditTag.Authentication]: "audits.tagLabels.authentication",
  [AuditTag.Authorization]:  "audits.tagLabels.authorization",
};

function fmtTagName(t: TranslateFn, flag: number): string {
  const key = TAG_I18N_KEY[flag];
  return key ? t(key) : String(flag);
}

function fmtDecodedTags(t: TranslateFn, mask: number): string[] {
  return AUDIT_TAG_LABELS
    .filter((tl) => (mask & tl.flag) !== 0)
    .map((tl) => fmtTagName(t, tl.flag));
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

// ════════════════════════════════════════════════════════════════════════
// Audit log — MakaGrid + MakaGridFilters, server-side paged/sorted, with a
// self-contained filter/KPI panel and detail drawer.
// ════════════════════════════════════════════════════════════════════════

type AuditRow = AuditSummaryDto & {
  actorName: string;
  eventTypeLabel: string;
  severityLabel: string;
  sourceText: string;
  entityText: string;
  occurredAt: Date;
  timeLabel: string;
  dateLabel: string;
  operationLabel: string;
};

// ── Cell templates (hook-free; read enriched fields) ──────────────────────
function AuditActorCell(row: AuditRow) {
  return (
    <div className="flex min-w-0 items-center gap-2.5">
      <EntityInitialsAvatar name={row.actorName} size={28} />
      <span className="truncate text-[13px] text-[var(--color-foreground)]">{row.actorName}</span>
    </div>
  );
}
function AuditEventCell(row: AuditRow) {
  const Icon = eventTypeIcon(row.eventType);
  const color = severityColorVar(row.severity);
  return (
    <span className="inline-flex items-center gap-1.5 text-[12.5px] text-[var(--color-foreground)]">
      <Icon className="size-3.5 shrink-0" style={{ color }} aria-hidden />
      {row.eventTypeLabel}
    </span>
  );
}
function AuditSeverityCell(row: AuditRow) {
  const tone = severityTone(row.severity);
  return (
    <EntityStatusBadge tone={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}>
      {row.severityLabel}
    </EntityStatusBadge>
  );
}
function AuditSourceCell(row: AuditRow) {
  return (
    <code className="block truncate font-mono text-[11.5px] text-[var(--color-muted-foreground)]">
      {row.sourceText}
    </code>
  );
}
function AuditEntityCell(row: AuditRow) {
  return (
    <span className="block truncate text-[12.5px] text-[var(--color-foreground)]">{row.entityText}</span>
  );
}
function AuditDateCell(row: AuditRow) {
  return (
    <div className="leading-tight">
      <div className="text-[12.5px] tabular-nums text-[var(--color-foreground)]">{row.timeLabel}</div>
      <div className="text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">{row.dateLabel}</div>
    </div>
  );
}
function AuditOperationCell(row: AuditRow) {
  if (!row.entityOperation) {
    return <span className="text-[12.5px] text-[var(--color-muted-foreground)]">—</span>;
  }
  return (
    <span
      className="inline-flex items-center rounded-md px-2 py-0.5 text-[11.5px] font-medium"
      style={operationBadgeStyle(row.entityOperation)}
    >
      {row.operationLabel}
    </span>
  );
}

// ── KPI card ──────────────────────────────────────────────────────────────
function AuditKpiCard({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="flex flex-col items-center rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-4 py-3 text-center">
      <div className="flex items-center justify-center gap-2">
        <span aria-hidden className="size-2 rounded-full" style={{ backgroundColor: tone }} />
        <span className="truncate text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {label}
        </span>
      </div>
      <div className="mt-1 font-display text-[26px] font-semibold leading-none tabular-nums text-[var(--color-foreground)]">
        {value.toLocaleString("es-CO")}
      </div>
    </div>
  );
}

function AuditsMakaSection({ panelOpen }: { panelOpen: boolean }) {
  const { t } = useTranslation("common");
  const { formatDate, formatTime } = useLocalization();

  const [resetKey, setResetKey] = useState(0);
  // Default to today's events.
  const [createdRange, setCreatedRange] = useState<MakaDateRange | null>(() => makaPresetRange("today"));
  // Stored as stringified enum values because EntityFilterPill keys on string.
  const [eventType, setEventType] = useState<string | null>(null);
  const [severity, setSeverity] = useState<string | null>(null);
  // Source / user / entity / operation are dropdowns → selected value or null.
  const [source, setSource] = useState<string | null>(null);
  const [userId, setUserId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [entityName, setEntityName] = useState<string | null>(null);
  const [operation, setOperation] = useState<string | null>(null);
  const [drawerId, setDrawerId] = useState<string | null>(null);

  // Server-side pagination + sort state (the grid pages/sorts against the API).
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sort, setSort] = useState<string | undefined>(undefined);

  const fromUtc = createdRange?.start.toISOString();
  const toUtc = createdRange?.end.toISOString();

  // Shared filter params for BOTH the paged list and the KPI summary so the
  // KPIs always describe exactly the rows the grid is showing.
  const filterParams = useMemo(
    () => ({
      fromUtc,
      toUtc,
      eventType: (eventType ? Number(eventType) : undefined) as AuditEventType | undefined,
      severity: (severity ? Number(severity) : undefined) as AuditSeverity | undefined,
      source: source || undefined,
      userId: userId || undefined,
      search: search || undefined,
      entityName: entityName || undefined,
      entityOperation: operation || undefined,
    }),
    [fromUtc, toUtc, eventType, severity, source, userId, search, entityName, operation],
  );

  // Any filter change resets paging back to the first page.
  useEffect(() => {
    setPage(1);
  }, [filterParams]);

  const resetFilters = () => {
    setEventType(null);
    setSeverity(null);
    setSource(null);
    setUserId(null);
    setSearch("");
    setEntityName(null);
    setOperation(null);
    setCreatedRange(makaPresetRange("today"));
    setPage(1);
    setSort(undefined);
    setResetKey((k) => k + 1);
  };

  // Maps a grid column field to the API sort field.
  const sortFieldFor = (field: string): string | undefined =>
    ({
      occurredAt: "occurredAtUtc",
      actorName: "userName",
      eventTypeLabel: "eventType",
      severityLabel: "severity",
      entityText: "entityName",
      sourceText: "source",
      operationLabel: "entityOperation",
    })[field];

  const listQuery = useQuery({
    queryKey: ["audits", "maka-list", filterParams, page, pageSize, sort],
    queryFn: ({ signal }) => listAudits({ pageNumber: page, pageSize, sort, ...filterParams }, signal),
    placeholderData: keepPreviousData,
    staleTime: 5_000,
    refetchOnWindowFocus: false,
  });

  const summaryQuery = useQuery({
    queryKey: ["audits", "maka-summary", filterParams],
    queryFn: ({ signal }) => getAuditSummary(filterParams, signal),
    staleTime: 30_000,
    refetchOnWindowFocus: false,
  });

  // Users for the user filter dropdown.
  const usersQuery = useQuery({
    queryKey: ["identity", "users", "audit-filter"],
    queryFn: () => searchUsers({ pageNumber: 1, pageSize: 100 }),
    staleTime: 5 * 60_000,
  });

  const rows: AuditRow[] = useMemo(
    () =>
      (listQuery.data?.items ?? []).map((a) => ({
        ...a,
        actorName: a.userName ?? (a.userId ? `${a.userId.slice(0, 8)}…` : t("audits.system")),
        eventTypeLabel: fmtEventType(t, a.eventType),
        severityLabel: fmtSeverity(t, a.severity),
        sourceText: a.source ?? "—",
        entityText: a.entityName ?? lastSegment(a.source),
        occurredAt: new Date(a.occurredAtUtc),
        timeLabel: formatTime(a.occurredAtUtc),
        dateLabel: formatDate(a.occurredAtUtc),
        operationLabel: a.entityOperation
          ? t(`audits.operations.${a.entityOperation.charAt(0).toLowerCase()}${a.entityOperation.slice(1)}`)
          : "—",
      })),
    [listQuery.data, t, formatTime, formatDate],
  );

  const kpis = useMemo(() => {
    const s = summaryQuery.data;
    const byType = s?.eventsByType ?? {};
    const bySev = s?.eventsBySeverity ?? {};
    const grand = Object.values(byType).reduce((a, b) => a + b, 0);
    // The API serializes enums by name ("Activity", "Information", ...); fall
    // back to the numeric key in case serialization changes.
    const pick = (
      bucket: Record<string, number>,
      name: string,
      num: number,
    ) => bucket[name] ?? bucket[String(num)] ?? 0;
    return {
      grand,
      activity: pick(byType, "Activity", AuditEventType.Activity),
      entity: pick(byType, "EntityChange", AuditEventType.EntityChange),
      security: pick(byType, "Security", AuditEventType.Security),
      exception: pick(byType, "Exception", AuditEventType.Exception),
      info: pick(bySev, "Information", AuditSeverity.Information),
      warn: pick(bySev, "Warning", AuditSeverity.Warning),
      err: pick(bySev, "Error", AuditSeverity.Error),
      crit: pick(bySev, "Critical", AuditSeverity.Critical),
    };
  }, [summaryQuery.data]);

  const eventOptions = useMemo(
    () => [
      { value: null as string | null, label: t("status.all") },
      { value: String(AuditEventType.Activity), label: fmtEventType(t, AuditEventType.Activity) },
      { value: String(AuditEventType.Security), label: fmtEventType(t, AuditEventType.Security) },
      { value: String(AuditEventType.EntityChange), label: fmtEventType(t, AuditEventType.EntityChange) },
      { value: String(AuditEventType.Exception), label: fmtEventType(t, AuditEventType.Exception) },
    ],
    [t],
  );
  const severityOptions = useMemo(
    () => [
      { value: null as string | null, label: t("status.all") },
      { value: String(AuditSeverity.Information), label: fmtSeverity(t, AuditSeverity.Information) },
      { value: String(AuditSeverity.Warning), label: fmtSeverity(t, AuditSeverity.Warning) },
      { value: String(AuditSeverity.Error), label: fmtSeverity(t, AuditSeverity.Error) },
      { value: String(AuditSeverity.Critical), label: fmtSeverity(t, AuditSeverity.Critical) },
    ],
    [t],
  );
  const entityOptions = useMemo(
    () => [
      { value: null as string | null, label: t("audits.allEntities") },
      ...(summaryQuery.data?.topEntityNames ?? []).map((name) => ({
        value: name as string | null,
        label: name,
      })),
    ],
    [t, summaryQuery.data?.topEntityNames],
  );
  const operationOptions = useMemo(
    () => [
      { value: null as string | null, label: t("audits.allOperations") },
      { value: "Insert", label: t("audits.operations.insert") },
      { value: "Update", label: t("audits.operations.update") },
      { value: "Delete", label: t("audits.operations.delete") },
      { value: "SoftDelete", label: t("audits.operations.softDelete") },
      { value: "Restore", label: t("audits.operations.restore") },
    ],
    [t],
  );
  const sourceOptions = useMemo(
    () => [
      { value: null as string | null, label: t("audits.allSources") },
      ...Object.keys(summaryQuery.data?.eventsBySource ?? {})
        .sort((a, b) => a.localeCompare(b))
        .map((src) => ({ value: src as string | null, label: src })),
    ],
    [t, summaryQuery.data?.eventsBySource],
  );
  const userOptions = useMemo(
    () => [
      { value: null as string | null, label: t("audits.allUsers") },
      ...(usersQuery.data?.items ?? [])
        .filter((u) => u.id)
        .map((u) => ({
          value: u.id as string | null,
          label:
            [u.firstName, u.lastName].filter(Boolean).join(" ").trim() ||
            u.userName ||
            u.email ||
            (u.id ?? ""),
        })),
    ],
    [t, usersQuery.data?.items],
  );

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "occurredAt", headerText: t("audits.columns.date"), template: AuditDateCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "actorName", headerText: t("audits.user"), template: AuditActorCell as any, minWidth: 180 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "eventTypeLabel", headerText: t("audits.columns.action"), template: AuditEventCell as any, width: 150 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "severityLabel", headerText: t("audits.severityLabel"), template: AuditSeverityCell as any, width: 120 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "entityText", headerText: t("audits.columns.entity"), template: AuditEntityCell as any, minWidth: 160 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "operationLabel", headerText: t("audits.entityOperation"), template: AuditOperationCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "sourceText", headerText: t("audits.source"), template: AuditSourceCell as any, minWidth: 160 },
    ],
    [t],
  );

  return (
    <section className="space-y-4">
      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            {/* Row 1 — date, action, severity, search (wide) */}
            <MakaFilterField label={t("audits.columns.date")}>
              <MakaDateRangePicker
                key={`range-${resetKey}`}
                defaultPreset="today"
                value={createdRange}
                onChange={setCreatedRange}
              />
            </MakaFilterField>
            <MakaFilterField label={t("audits.columns.action")}>
              <EntityFilterPill<string | null> label={t("audits.columns.action")} value={eventType} onChange={setEventType} options={eventOptions} />
            </MakaFilterField>
            <MakaFilterField label={t("audits.severityLabel")}>
              <EntityFilterPill<string | null> label={t("audits.severityLabel")} value={severity} onChange={setSeverity} options={severityOptions} />
            </MakaFilterField>
            <MakaFilterField label={t("audits.search")} className="grow">
              <MakaFilterInput value={search} onChange={setSearch} placeholder={t("audits.searchPlaceholder")} ariaLabel={t("audits.search")} className="min-w-72" />
            </MakaFilterField>

            {/* Force a new row → source, user, entity, operation below */}
            <div className="basis-full" aria-hidden />

            {/* Row 2 — source, user, entity, operation (clearable dropdowns) */}
            <MakaFilterField label={t("audits.source")}>
              <Combobox
                id="audit-source"
                label={t("audits.source")}
                placeholder={t("audits.allSources")}
                value={source}
                onChange={setSource}
                options={sourceOptions.filter((o) => o.value !== null).map((o) => ({ value: o.value as string, label: o.label }))}
                searchable
                clearable
                emptyOptionLabel={t("audits.allSources")}
                className={AUDIT_FILTER_COMBO}
              />
            </MakaFilterField>
            <MakaFilterField label={t("audits.user")}>
              <Combobox
                id="audit-user"
                label={t("audits.user")}
                placeholder={t("audits.allUsers")}
                value={userId}
                onChange={setUserId}
                options={userOptions.filter((o) => o.value !== null).map((o) => ({ value: o.value as string, label: o.label }))}
                searchable
                clearable
                emptyOptionLabel={t("audits.allUsers")}
                className={AUDIT_FILTER_COMBO}
              />
            </MakaFilterField>
            <MakaFilterField label={t("audits.entityName")}>
              <Combobox
                id="audit-entity"
                label={t("audits.entityName")}
                placeholder={t("audits.allEntities")}
                value={entityName}
                onChange={setEntityName}
                options={entityOptions.filter((o) => o.value !== null).map((o) => ({ value: o.value as string, label: o.label }))}
                searchable
                clearable
                emptyOptionLabel={t("audits.allEntities")}
                className={AUDIT_FILTER_COMBO}
              />
            </MakaFilterField>
            <MakaFilterField label={t("audits.entityOperation")}>
              <Combobox
                id="audit-operation"
                label={t("audits.entityOperation")}
                placeholder={t("audits.allOperations")}
                value={operation}
                onChange={setOperation}
                options={operationOptions.filter((o) => o.value !== null).map((o) => ({ value: o.value as string, label: o.label }))}
                clearable
                emptyOptionLabel={t("audits.allOperations")}
                className={AUDIT_FILTER_COMBO}
              />
            </MakaFilterField>
          </>
        }
        kpis={
          <>
            <AuditKpiCard label={t("audits.total")} value={kpis.grand} tone="var(--color-primary)" />
            <AuditKpiCard label={fmtEventType(t, AuditEventType.Activity)} value={kpis.activity} tone="var(--color-info)" />
            <AuditKpiCard label={fmtEventType(t, AuditEventType.EntityChange)} value={kpis.entity} tone="var(--color-chart-2)" />
            <AuditKpiCard label={fmtEventType(t, AuditEventType.Security)} value={kpis.security} tone="var(--color-warning)" />
            <AuditKpiCard label={fmtEventType(t, AuditEventType.Exception)} value={kpis.exception} tone="var(--color-destructive)" />
            <AuditKpiCard label={fmtSeverity(t, AuditSeverity.Information)} value={kpis.info} tone="var(--color-info)" />
            <AuditKpiCard label={fmtSeverity(t, AuditSeverity.Warning)} value={kpis.warn} tone="var(--color-warning)" />
            <AuditKpiCard label={fmtSeverity(t, AuditSeverity.Error)} value={kpis.err} tone="var(--color-destructive)" />
            <AuditKpiCard label={fmtSeverity(t, AuditSeverity.Critical)} value={kpis.crit} tone="var(--color-destructive)" />
          </>
        }
      />

      <MakaGridServer<AuditRow>
        dataSource={rows}
        columns={columns}
        isLoading={listQuery.isFetching}
        fileName="auditoria"
        entityName={t("audits.unit")}
        onRowClick={(row) => setDrawerId(row.id)}
        onClearFilters={resetFilters}
        serverPaging={{
          totalCount: listQuery.data?.totalCount ?? 0,
          page,
          pageSize,
          pageSizes: [20, 50, 100],
          onChange: ({ page: p, pageSize: ps }) => {
            setPage(p);
            setPageSize(ps);
          },
          onSortChange: (s) => {
            setPage(1);
            if (!s) {
              setSort(undefined);
            } else {
              const apiField = sortFieldFor(s.field);
              setSort(apiField ? `${apiField} ${s.dir}` : undefined);
            }
          },
        }}
      />

      <AuditDetailDrawer
        auditId={drawerId}
        onClose={() => setDrawerId(null)}
        onJumpAudit={(id) => setDrawerId(id)}
        onJumpCorrelation={() => setDrawerId(null)}
        onJumpTrace={() => setDrawerId(null)}
      />
    </section>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function AuditsPage() {
  const { t } = useTranslation("common");
  const [panelOpen, setPanelOpen] = useState(true);
  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ScrollText}
        title={t("audits.title")}
        unit={t("audits.unit")}
        unitPlural={t("audits.unitPlural")}
        description={t("audits.description")}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {t("gridFilters.panelToggle")}
        </Button>
      </EntityPageHeader>
      <AuditsMakaSection panelOpen={panelOpen} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card — patient-style: avatar + actor + secondary line + action.
// ───────────────────────────────────────────────────────────────────────

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
  const tags = fmtDecodedTags(t, detail.tags);

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
            {fmtSeverity(t, detail.severity)}
          </Badge>
          <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {fmtEventType(t, detail.eventType)}
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
            {tags.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-2 py-0.5 font-mono text-[10.5px]"
              >
                <Tag className="h-2.5 w-2.5" />
                {tag}
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
                        {row.source ?? fmtEventType(t, row.eventType)}
                      </span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
                        {fmtSeverity(t, row.severity)}
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
