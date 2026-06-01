import { useMemo, useRef, useState } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Eye,
  Globe,
  LogOut,
  MonitorSmartphone,
  RefreshCw,
  ShieldCheck,
  Smartphone,
  UserCog,
} from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSessionById,
  getTenantSessions,
  type UserSessionDto,
} from "@/api/sessions";
import { Button } from "@/components/ui/button";
import { P } from "@/auth/permissions";
import {
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import { useAuth } from "@/auth/use-auth";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { describe, formatRelative } from "@/lib/list-helpers";

// Fetch a large client-side page; MakaGridClient handles paging/sort/filter.
const PAGE_SIZE = 100;

type SessionRow = {
  id: string;
  userId: string | null;
  isActive: boolean;
  isCurrentSession: boolean;
  isMobile: boolean;
  displayName: string;
  email: string;
  browser: string;
  os: string;
  ip: string;
  lastActivityLabel: string;
  youLabel: string;
  inactiveLabel: string;
  raw: UserSessionDto;
};

// Mutable context the (stable, hook-free) cell templates read at call time.
type ActionsCtx = {
  revoke: (s: UserSessionDto) => void;
  revokingId: string | null;
  revokeAll: (userId: string) => void;
  revokingUserId: string | null;
  labels: {
    revoke: string;
    revoking: string;
    allDevices: string;
    allDevicesTitle: string;
  };
};

// ───────────────────────────────────────────────────────────────────────
//  Page — admin / tenant-wide sessions console
// ───────────────────────────────────────────────────────────────────────

export function SessionsPage() {
  const { t } = useTranslation("system");
  const { t: tc } = useTranslation("common");
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");
  const [includeInactive, setIncludeInactive] = useState(false);

  const query = useQuery({
    queryKey: ["identity", "sessions", "tenant", { includeInactive }],
    queryFn: () =>
      getTenantSessions({ includeInactive, pageNumber: 1, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000, // light auto-refresh — sessions move fast
  });

  const items = useMemo(() => query.data?.items ?? [], [query.data]);

  const revokeOne = useMutation({
    mutationFn: (s: UserSessionDto) => adminRevokeUserSessionById(s.userId ?? "", s.id),
    onSuccess: () => {
      toast.success(t("sessions.revokeSuccess"));
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions"] });
    },
    onError: (err) =>
      toast.error(
        err instanceof ApiRequestError ? err.problem?.detail ?? err.message : t("sessions.revokeErrorFallback"),
      ),
  });

  const revokeAllForUser = useMutation({
    mutationFn: (userId: string) => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(t("sessions.revokeAllSuccess", { count: data.revokedCount }));
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions"] });
    },
    onError: (err) =>
      toast.error(
        err instanceof ApiRequestError ? err.problem?.detail ?? err.message : t("sessions.revokeAllErrorFallback"),
      ),
  });

  // Keep the cell templates' context current without rebuilding the templates.
  const ctxRef = useRef<ActionsCtx>({
    revoke: () => {},
    revokingId: null,
    revokeAll: () => {},
    revokingUserId: null,
    labels: { revoke: "", revoking: "", allDevices: "", allDevicesTitle: "" },
  });
  ctxRef.current = {
    revoke: (s) => revokeOne.mutate(s),
    revokingId: revokeOne.isPending ? (revokeOne.variables?.id ?? null) : null,
    revokeAll: (uid) => revokeAllForUser.mutate(uid),
    revokingUserId: revokeAllForUser.isPending ? (revokeAllForUser.variables ?? null) : null,
    labels: {
      revoke: t("sessions.revoke"),
      revoking: t("sessions.revoking"),
      allDevices: t("sessions.allDevicesShort"),
      allDevicesTitle: t("sessions.allDevicesTitle"),
    },
  };

  const rows: SessionRow[] = useMemo(() => {
    const q = search.trim().toLowerCase();
    return items
      .map<SessionRow>((s) => ({
        id: s.id,
        userId: s.userId ?? null,
        isActive: s.isActive,
        isCurrentSession: s.isCurrentSession,
        isMobile: (s.deviceType ?? "").toLowerCase().includes("mobile"),
        displayName: s.userName ?? s.userEmail ?? t("sessions.unknownUser"),
        email: s.userName && s.userEmail ? s.userEmail : "",
        browser:
          [s.browser, s.browserVersion].filter(Boolean).join(" ") || t("sessions.unknownBrowser"),
        os: [s.operatingSystem, s.osVersion].filter(Boolean).join(" "),
        ip: s.ipAddress ?? "—",
        lastActivityLabel: formatRelative(s.lastActivityAt),
        youLabel: t("sessions.youBadge"),
        inactiveLabel: t("sessions.inactiveBadge"),
        raw: s,
      }))
      .filter((r) => {
        if (!q) return true;
        return [r.displayName, r.email, r.browser, r.os, r.ip]
          .filter(Boolean)
          .some((v) => v.toLowerCase().includes(q));
      });
  }, [items, search, t]);

  // ── Cell templates (stable; read ctxRef at call time) ──────────────────
  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "displayName", headerText: t("sessions.cols.user"), template: SessionUserCell as any, minWidth: 220 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "browser", headerText: t("sessions.cols.device"), template: SessionDeviceCell as any, minWidth: 200, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "ip", headerText: t("sessions.cols.ip"), template: SessionIpCell as any, width: 150 },
      { field: "lastActivityLabel", headerText: t("sessions.cols.lastActivity"), width: 150, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "__actions", headerText: t("sessions.cols.actions"), template: makeActionsCell(ctxRef) as any, width: 170, textAlign: "Right", headerTextAlign: "Right", allowSorting: false, allowFiltering: false },
    ],
    [t],
  );

  const filterOptions = [
    { value: false, label: t("sessions.filterLiveOnly") },
    { value: true, label: t("sessions.filterIncludeInactive") },
  ];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UserCog}
        title={t("sessions.title")}
        total={query.data?.totalCount ?? null}
        unit={t("sessions.unit")}
        unitPlural={t("sessions.unitPlural")}
        description={t("sessions.description", { tenant: user?.tenant ?? "this tenant" })}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {tc("gridFilters.filtersTab")}
        </Button>
        <Button
          variant="outline"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <RefreshCw className={cn("size-4", query.isFetching && "animate-spin")} />
          {t("sessions.refresh")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={() => {
          setSearch("");
          setIncludeInactive(false);
        }}
        filters={
          <>
            <MakaFilterField label={tc("gridFilters.search")} className="grow">
              <MakaFilterInput
                value={search}
                onChange={setSearch}
                placeholder={t("sessions.searchPlaceholder")}
                ariaLabel={tc("gridFilters.search")}
                className="min-w-64"
              />
            </MakaFilterField>
            <MakaFilterField label={t("sessions.filterVisibility")}>
              <EntityFilterPill<boolean>
                label={t("sessions.filterVisibility")}
                value={includeInactive}
                onChange={setIncludeInactive}
                options={filterOptions}
              />
            </MakaFilterField>
          </>
        }
      />

      <MakaGridClient<SessionRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isLoading}
        fileName="sesiones"
        entityName={t("sessions.unit")}
        onClearFilters={() => {
          setSearch("");
          setIncludeInactive(false);
        }}
      />

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <span>{describe(query.error)}</span>
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Cell templates (hook-free; read enriched row fields / ctxRef)
// ───────────────────────────────────────────────────────────────────────

function SessionUserCell(row: SessionRow) {
  return (
    <div className={cn("flex min-w-0 items-center gap-3", !row.isActive && "opacity-75")}>
      <EntityInitialsAvatar name={row.displayName} size={32} />
      <div className="min-w-0">
        <div className="flex items-center gap-1.5">
          <span className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.displayName}</span>
          {row.isCurrentSession && <EntityStatusBadge tone="info">{row.youLabel}</EntityStatusBadge>}
          {!row.isActive && <EntityStatusBadge tone="danger">{row.inactiveLabel}</EntityStatusBadge>}
        </div>
        {row.email && (
          <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">{row.email}</code>
        )}
      </div>
    </div>
  );
}

function SessionDeviceCell(row: SessionRow) {
  const DeviceIcon = row.isMobile ? Smartphone : MonitorSmartphone;
  return (
    <div className="flex min-w-0 items-center gap-1.5 text-[12.5px] text-[var(--color-foreground)]">
      <DeviceIcon className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
      <div className="min-w-0">
        <div className="truncate">{row.browser}</div>
        {row.os && <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">{row.os}</div>}
      </div>
    </div>
  );
}

function SessionIpCell(row: SessionRow) {
  return <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">{row.ip}</code>;
}

function makeActionsCell(ctxRef: React.MutableRefObject<ActionsCtx>) {
  // eslint-disable-next-line react/display-name
  return function SessionActionsCell(row: SessionRow) {
    const ctx = ctxRef.current;
    if (!row.isActive || row.isCurrentSession) {
      return <span className="text-[11px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">—</span>;
    }
    return (
      <div className="flex items-center justify-end gap-1.5" onClick={(e) => e.stopPropagation()}>
        {row.userId && (
          <Button
            perm={P.identity.sessions.revokeAll}
            variant="ghost"
            size="sm"
            disabled={ctx.revokingUserId === row.userId}
            onClick={() => row.userId && ctx.revokeAll(row.userId)}
            className="gap-1.5 text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
            title={ctx.labels.allDevicesTitle}
          >
            <ShieldCheck className="size-3.5" />
            <span className="hidden lg:inline">{ctx.labels.allDevices}</span>
          </Button>
        )}
        <Button
          perm={P.identity.sessions.revokeAll}
          variant="outline"
          size="sm"
          disabled={ctx.revokingId === row.id}
          onClick={() => ctx.revoke(row.raw)}
          className="gap-1.5"
        >
          <LogOut className="size-3.5" />
          {ctx.revokingId === row.id ? ctx.labels.revoking : ctx.labels.revoke}
        </Button>
      </div>
    );
  };
}

// Globe reserved for a future per-row geo-IP indicator.
void Globe;
