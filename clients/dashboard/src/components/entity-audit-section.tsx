import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { useLocalization } from "@/contexts/localization-context";
import { ExternalLink, History } from "lucide-react";
import { Link } from "react-router-dom";
import {
  listAudits,
  AuditEventType,
  type AuditSummaryDto,
} from "@/api/audits";
import { Skeleton } from "@/components/ui/skeleton";

// ────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────

type OperationColors = { bg: string; text: string; border: string };

function operationColors(op: string | null | undefined): OperationColors {
  switch (op) {
    case "Insert":
      return {
        bg: "oklch(from var(--color-success) l c h / 0.12)",
        text: "var(--color-success)",
        border: "oklch(from var(--color-success) l c h / 0.30)",
      };
    case "Update":
      return {
        bg: "oklch(from var(--color-info) l c h / 0.12)",
        text: "var(--color-info)",
        border: "oklch(from var(--color-info) l c h / 0.30)",
      };
    case "Delete":
      return {
        bg: "oklch(from var(--color-destructive) l c h / 0.12)",
        text: "var(--color-destructive)",
        border: "oklch(from var(--color-destructive) l c h / 0.30)",
      };
    case "SoftDelete":
      return {
        bg: "oklch(from var(--color-warning) l c h / 0.12)",
        text: "var(--color-warning)",
        border: "oklch(from var(--color-warning) l c h / 0.30)",
      };
    case "Restore":
      return {
        bg: "oklch(from var(--color-accent) l c h / 0.20)",
        text: "var(--color-accent)",
        border: "oklch(from var(--color-accent) l c h / 0.40)",
      };
    default:
      return {
        bg: "oklch(from var(--color-muted-foreground) l c h / 0.10)",
        text: "var(--color-muted-foreground)",
        border: "oklch(from var(--color-muted-foreground) l c h / 0.20)",
      };
  }
}

function operationLabel(
  op: string | null | undefined,
  t: (k: string) => string,
): string {
  switch (op) {
    case "Insert":
      return t("audits.operations.insert");
    case "Update":
      return t("audits.operations.update");
    case "Delete":
      return t("audits.operations.delete");
    case "SoftDelete":
      return t("audits.operations.softDelete");
    case "Restore":
      return t("audits.operations.restore");
    default:
      return op ?? "—";
  }
}

/** Compact relative time — "42s", "5m", "3h", "2d" */
function fmtRelativeCompact(iso: string): string {
  const delta = Math.max(0, Math.floor((Date.now() - Date.parse(iso)) / 1000));
  if (delta < 60) return `${delta}s`;
  const m = Math.floor(delta / 60);
  if (m < 60) return `${m}m`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h`;
  return `${Math.floor(h / 24)}d`;
}


// ────────────────────────────────────────────────────────────────────────
// OperationBadge — pill chip with semantic colour tint.
// ────────────────────────────────────────────────────────────────────────

function OperationBadge({
  op,
  label,
}: {
  op: string | null | undefined;
  label: string;
}) {
  const c = operationColors(op);
  return (
    <span
      className="inline-flex items-center rounded-full border px-1.5 py-px font-mono text-[10px] font-semibold leading-none"
      style={{ background: c.bg, color: c.text, borderColor: c.border }}
    >
      {label}
    </span>
  );
}

// ────────────────────────────────────────────────────────────────────────
// EntityAuditSection — condensed change history for a single entity.
//
// Renders the last `limit` EntityChange audit events for the given
// entityKey as a compact vertical timeline. Used inside edit dialogs and
// detail pages so the operator can see who last touched a record without
// leaving the current view.
//
// Props:
//   entityKey  — the entity's primary key UUID. Required.
//   entityName — optional display name used only for the query key; the
//                API filters by entityKey which is already exact.
//   limit      — maximum rows to fetch and render (default: 8).
// ────────────────────────────────────────────────────────────────────────

export function EntityAuditSection({
  entityKey,
  entityName,
  limit = 8,
}: {
  entityKey: string;
  entityName?: string;
  limit?: number;
}) {
  const { t } = useTranslation("settings");

  const query = useQuery({
    queryKey: ["audits", "entity", entityKey, entityName, limit],
    queryFn: ({ signal }) =>
      listAudits(
        {
          // Backend stores EntityKey as "Id:{uuid}" (from EntityDiffBuilder.BuildKey).
          // We must match that format exactly — the filter is an exact-string match.
          entityKey: `Id:${entityKey}`,
          eventType: AuditEventType.EntityChange,
          pageSize: limit,
          pageNumber: 1,
        },
        signal,
      ),
    enabled: !!entityKey,
    staleTime: 30_000,
  });

  const items = query.data?.items ?? [];

  return (
    <section className="space-y-2">
      {/* ── Header ─────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <History className="size-3.5 text-[var(--color-muted-foreground)]" />
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.1em] text-[var(--color-muted-foreground)]">
            {t("audits.section.title")}
          </span>
        </div>

        <Link
          to="/system/audits"
          className="flex items-center gap-1 font-mono text-[10.5px] text-[var(--color-primary)] hover:underline"
        >
          {t("audits.section.viewAll")}
          <ExternalLink className="size-3" />
        </Link>
      </div>

      {/* ── Body ───────────────────────────────────────────────────── */}
      {query.isLoading ? (
        <div className="space-y-1.5">
          {Array.from({ length: 3 }).map((_, i) => (
            // eslint-disable-next-line react/no-array-index-key
            <Skeleton key={i} className="h-9 w-full rounded-lg" />
          ))}
        </div>
      ) : query.isError ? (
        <p className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.25)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 font-mono text-[11px] text-[var(--color-destructive)]">
          {t("audits.section.errorLoading")}
        </p>
      ) : items.length === 0 ? (
        <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {t("audits.section.empty")}
        </p>
      ) : (
        <ol className="space-y-0">
          {items.map((item, i) => (
            <AuditTimelineRow
              key={item.id}
              item={item}
              isLast={i === items.length - 1}
            />
          ))}
        </ol>
      )}
    </section>
  );
}

// ────────────────────────────────────────────────────────────────────────
// AuditTimelineRow — single row in the timeline.
// ────────────────────────────────────────────────────────────────────────

function AuditTimelineRow({
  item,
  isLast,
}: {
  item: AuditSummaryDto;
  isLast: boolean;
}) {
  // Both section title ("Historial de cambios", "por") and operation labels
  // ("Creación", "Actualización"…) live in the settings namespace.
  const { t } = useTranslation("settings");
  const { formatDateTime } = useLocalization();

  const colors = operationColors(item.entityOperation);
  const actor =
    item.userName ??
    (item.userId ? `${item.userId.slice(0, 8)}…` : "System");
  const relTime = fmtRelativeCompact(item.occurredAtUtc);
  const fullTime = formatDateTime(item.occurredAtUtc);

  return (
    <li className="flex items-start gap-2.5 py-1.5">
      {/* Vertical timeline dot + connector */}
      <div className="flex flex-col items-center self-stretch pt-[5px]">
        <span
          aria-hidden
          className="inline-block size-2 shrink-0 rounded-full ring-2 ring-[var(--color-card)]"
          style={{ backgroundColor: colors.text }}
        />
        {!isLast && (
          <span
            aria-hidden
            className="mt-1 w-px flex-1 bg-[var(--color-border)]"
          />
        )}
      </div>

      {/* Text content */}
      <div className="min-w-0 flex-1 pb-1">
        <div className="flex items-center justify-between gap-2">
          {/* Operation badge */}
          <OperationBadge
            op={item.entityOperation}
            label={operationLabel(item.entityOperation, t)}
          />
          {/* Relative time with full local timestamp on hover */}
          <span
            className="shrink-0 cursor-default font-mono text-[10px] tabular-nums text-[var(--color-muted-foreground)]"
            title={fullTime}
          >
            {relTime}
          </span>
        </div>
        <p className="mt-0.5 truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          {t("audits.section.by")} {actor}
        </p>
      </div>
    </li>
  );
}
