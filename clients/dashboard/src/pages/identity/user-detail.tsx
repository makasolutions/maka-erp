import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  CheckCircle2,
  CircleSlash2,
  Clock,
  Globe,
  Mail,
  MonitorSmartphone,
  Phone,
  Power,
  PowerOff,
  ShieldAlert,
  ShieldCheck,
  Smartphone,
  Trash2,
  User as UserIcon,
  UserCog,
  Users as UsersIcon,
  XCircle,
} from "lucide-react";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSession,
  assignUserRoles,
  deleteUser,
  getUserById,
  getUserRoles,
  getUserSessionsAdmin,
  toggleUserStatus,
  type AdminUserSessionDto,
  type UserRoleDto,
} from "@/api/identity";
import { useAuth } from "@/auth/use-auth";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailMeta,
  EntityDetailSection,
  EntityDetailStat,
  ErrorBand,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";
import { EntityAuditSection } from "@/components/entity-audit-section";

type DialogState =
  | { mode: "closed" }
  | { mode: "delete" }
  | { mode: "toggle-status" }
  | { mode: "impersonate" }
  | { mode: "revoke-all-sessions" };

function fullName(u: { firstName?: string; lastName?: string; userName?: string; email?: string }, fallback: string): string {
  const parts = [u.firstName, u.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return u.userName ?? u.email ?? fallback;
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function UserDetailPage() {
  const { t, i18n } = useTranslation("identity");
  const { userId = "" } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: actor, beginImpersonation } = useAuth();
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });
  const [impersonationReason, setImpersonationReason] = useState("");
  const [pending, setPending] = useState<Map<string, boolean>>(new Map());

  const { can } = usePerm();
  const canImpersonate = can(P.identity.users.impersonate);
  const canViewSessions = can(P.identity.sessions.viewAll);
  const canRevokeSessions = can(P.identity.sessions.revokeAll);

  const userQuery = useQuery({
    queryKey: ["identity", "users", userId],
    queryFn: () => getUserById(userId),
    enabled: !!userId,
  });

  const rolesQuery = useQuery({
    queryKey: ["identity", "users", userId, "roles"],
    queryFn: () => getUserRoles(userId),
    enabled: !!userId,
  });

  const user = userQuery.data;
  // useMemo is critical here: `rolesQuery.data ?? []` creates a new array
  // reference on every render when data is undefined. Without memoization
  // the useEffect below (which resets pending on [roles] change) fires on
  // every render → setPending → re-render → new [] → infinite loop (1000+
  // "Maximum update depth exceeded" errors in the console).
  const roles = useMemo(() => rolesQuery.data ?? [], [rolesQuery.data]);

  // Reset pending changes when fresh data arrives
  useEffect(() => {
    setPending(new Map());
  }, [roles]);

  const effective = (role: UserRoleDto) => {
    if (!role.roleId) return role.enabled;
    return pending.has(role.roleId) ? !!pending.get(role.roleId) : role.enabled;
  };

  const dirtyIds = useMemo(() => {
    const out: string[] = [];
    for (const r of roles) {
      if (!r.roleId) continue;
      if (pending.has(r.roleId) && pending.get(r.roleId) !== r.enabled) {
        out.push(r.roleId);
      }
    }
    return out;
  }, [roles, pending]);

  const isDirty = dirtyIds.length > 0;

  const toggle = (role: UserRoleDto) => {
    if (!role.roleId) return;
    setPending((prev) => {
      const next = new Map(prev);
      const current = next.has(role.roleId!) ? !!next.get(role.roleId!) : role.enabled;
      next.set(role.roleId!, !current);
      return next;
    });
  };

  const saveRoles = useMutation({
    mutationFn: () => {
      const payload: UserRoleDto[] = roles.map((r) => ({
        roleId: r.roleId,
        roleName: r.roleName,
        description: r.description,
        enabled: effective(r),
      }));
      return assignUserRoles(userId, payload);
    },
    onSuccess: () => {
      toast.success(t("users.detail.rolesUpdated"), {
        description: t("users.detail.rolesChangedCount", { count: dirtyIds.length }),
      });
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "roles"],
      });
    },
    onError: (err) => toast.error(t("users.detail.updateFailed"), { description: describe(err) }),
  });

  const toggleStatus = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return toggleUserStatus(user.id, !user.isActive);
    },
    onSuccess: () => {
      toast.success(user?.isActive ? t("users.detail.deactivated") : t("users.detail.reactivated"));
      void queryClient.invalidateQueries({ queryKey: ["identity", "users", userId] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      setDialog({ mode: "closed" });
    },
    onError: (err) => toast.error(t("users.detail.statusChangeFailed"), { description: describe(err) }),
  });

  const removeUser = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      return deleteUser(user.id);
    },
    onSuccess: () => {
      toast.success(t("users.detail.deleted"));
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      navigate("/identity/users");
    },
    onError: (err) => {
      toast.error(t("users.detail.deleteFailed"), { description: describe(err) });
      setDialog({ mode: "closed" });
    },
  });

  // Admin sessions
  const sessionsQuery = useQuery({
    queryKey: ["identity", "users", userId, "sessions"],
    queryFn: () => getUserSessionsAdmin(userId),
    enabled: !!userId && canViewSessions,
    staleTime: 15_000,
  });

  const revokeOne = useMutation({
    mutationFn: (sessionId: string) => adminRevokeUserSession(userId, sessionId),
    onSuccess: () => {
      toast.success(t("common:actions.revoke"));
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "sessions"],
      });
    },
    onError: (err) => toast.error(t("users.detail.updateFailed"), { description: describe(err) }),
  });

  const revokeAll = useMutation({
    mutationFn: () => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(
        t("users.sessionsCount", { count: data.revokedCount }) + " " + t("users.detail.revoking"),
      );
      void queryClient.invalidateQueries({
        queryKey: ["identity", "users", userId, "sessions"],
      });
      setDialog({ mode: "closed" });
    },
    onError: (err) => toast.error(t("users.detail.updateFailed"), { description: describe(err) }),
  });

  // Impersonation
  const impersonate = useMutation({
    mutationFn: () => {
      if (!user?.id) throw new Error("Missing user id");
      if (!actor?.tenant) throw new Error("No tenant on current session");
      return beginImpersonation({
        targetUserId: user.id,
        targetTenantId: actor.tenant,
        reason: impersonationReason.trim() || undefined,
      });
    },
    onSuccess: () => {
      toast.success(t("users.detail.impersonateStarted"), {
        description: t("users.detail.impersonateStartedDesc"),
      });
      setDialog({ mode: "closed" });
      setImpersonationReason("");
      navigate("/", { replace: true });
    },
    onError: (err) => {
      toast.error(t("users.detail.impersonateFailed"), { description: describe(err) });
    },
  });

  const locale = i18n.resolvedLanguage ?? "en";

  if (userQuery.isLoading) {
    return (
      <div className="space-y-6">
        <EntityDetailBack to="/identity/users" label={t("users.backToUsers")} />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (userQuery.isError || !user) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/users" label={t("users.backToUsers")} />
        <ErrorBand
          message={
            userQuery.error
              ? describe(userQuery.error)
              : t("users.notFound")
          }
        />
      </div>
    );
  }

  const display = fullName(user, t("users.noName"));
  const activeRolesCount = roles.filter((r) => effective(r)).length;
  const sessions = sessionsQuery.data ?? [];
  const activeSessionsCount = sessions.filter((s) => s.isActive).length;
  const subtitleParts: string[] = [];
  if (user.userName) subtitleParts.push(`@${user.userName}`);
  if (user.email) subtitleParts.push(user.email);
  if (user.phoneNumber) subtitleParts.push(user.phoneNumber);

  return (
    <div className="space-y-5 pb-12">
      <EntityDetailBack to="/identity/users" label={t("users.backToUsers")} />

      <EntityDetailHero
        avatar={
          <EntityDetailAvatar
            name={display}
            src={user.imageUrl ?? undefined}
          />
        }
        title={display}
        badges={
          <>
            {user.isActive ? (
              <Badge variant="success">
                <ShieldCheck className="h-3 w-3" /> {t("users.filters.active")}
              </Badge>
            ) : (
              <Badge variant="outline">
                <CircleSlash2 className="h-3 w-3" /> {t("users.filters.inactive")}
              </Badge>
            )}
            {user.emailConfirmed ? (
              <Badge variant="brand">
                <CheckCircle2 className="h-3 w-3" /> {t("users.emailConfirmedBadge")}
              </Badge>
            ) : (
              <Badge variant="warning">
                <Mail className="h-3 w-3" /> {t("users.emailPendingBadge")}
              </Badge>
            )}
          </>
        }
        subtitle={subtitleParts.join(" · ") || t("users.member")}
        actions={
          <>
            {canImpersonate && user.id !== actor?.id && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setDialog({ mode: "impersonate" })}
                disabled={!user.isActive}
                title={!user.isActive ? t("users.cannotImpersonateInactive") : undefined}
                className="gap-1.5"
              >
                <UserCog className="h-3.5 w-3.5" /> {t("users.impersonate")}
              </Button>
            )}
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDialog({ mode: "toggle-status" })}
            >
              {user.isActive ? (
                <>
                  <PowerOff className="mr-1 h-3.5 w-3.5" /> {t("common:actions.deactivate")}
                </>
              ) : (
                <>
                  <Power className="mr-1 h-3.5 w-3.5" /> {t("common:actions.reactivate")}
                </>
              )}
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setDialog({ mode: "delete" })}
            >
              <Trash2 className="mr-1 h-3.5 w-3.5" /> {t("common:actions.delete")}
            </Button>
          </>
        }
        stats={
          <>
            <EntityDetailStat
              icon={ShieldCheck}
              value={activeRolesCount}
              label={t("users.rolesCount", { count: activeRolesCount })}
              tone="primary"
            />
            {canViewSessions && (
              <EntityDetailStat
                icon={MonitorSmartphone}
                value={activeSessionsCount}
                label={t("users.sessionsCount", { count: activeSessionsCount })}
                tone={activeSessionsCount > 0 ? "success" : "default"}
              />
            )}
          </>
        }
        meta={
          <>
            {user.email && (
              <EntityDetailMeta icon={Mail} hideOnMobile>
                {user.email}
              </EntityDetailMeta>
            )}
            {user.phoneNumber && (
              <EntityDetailMeta icon={Phone} hideOnMobile>
                {user.phoneNumber}
              </EntityDetailMeta>
            )}
          </>
        }
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Profile */}
        <EntityDetailSection
          title={t("users.detail.identityCard")}
          icon={UserIcon}
          description={t("users.detail.identityCardDesc")}
        >
          <div className="space-y-3">
            <ProfileRow label={t("users.fields.username")} value={user.userName ?? "—"} />
            <ProfileRow label={t("users.fields.email")} value={user.email ?? "—"} />
            <ProfileRow label={t("users.fields.firstName")} value={user.firstName ?? "—"} />
            <ProfileRow label={t("users.fields.lastName")} value={user.lastName ?? "—"} />
            <ProfileRow label={t("users.fields.phone")} value={user.phoneNumber ?? "—"} />
            <ProfileRow
              label="ID"
              value={<span className="font-mono text-[11px]">{user.id}</span>}
            />
          </div>
        </EntityDetailSection>

        {/* Roles */}
        <EntityDetailSection
          title={t("users.detail.roleAssignment")}
          icon={ShieldCheck}
          description={t("users.detail.roleAssignmentDesc")}
          action={
            isDirty ? (
              <Badge variant="warning">{t("users.detail.pendingChanges", { count: dirtyIds.length })}</Badge>
            ) : undefined
          }
          padded={false}
          footer={
            roles.length > 0 ? (
              <div className="flex items-center justify-end gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPending(new Map())}
                  disabled={!isDirty || saveRoles.isPending}
                >
                  {t("common:actions.discard")}
                </Button>
                <Button
                  size="sm"
                  onClick={() => saveRoles.mutate()}
                  disabled={!isDirty || saveRoles.isPending}
                >
                  {saveRoles.isPending ? t("common:feedback.saving") : t("users.detail.saveChanges")}
                </Button>
              </div>
            ) : undefined
          }
        >
          {rolesQuery.isLoading ? (
            <div className="space-y-3 p-5">
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
              <Skeleton className="h-12 w-full rounded-md" />
            </div>
          ) : rolesQuery.isError ? (
            <div className="p-5">
              <ErrorBand message={describe(rolesQuery.error)} />
            </div>
          ) : roles.length === 0 ? (
            <div className="p-5 text-sm text-[var(--color-muted-foreground)]">
              {t("users.detail.noRoles")}{" "}
              <Link to="/identity/roles" className="underline hover:text-[var(--color-foreground)]">
                {t("users.detail.createRole")}
              </Link>{" "}
              {t("users.detail.toGrantAccess")}
            </div>
          ) : (
            <ul>
              {roles.map((role) => {
                const isOn = effective(role);
                const dirty =
                  role.roleId !== undefined &&
                  pending.has(role.roleId) &&
                  pending.get(role.roleId) !== role.enabled;
                return (
                  <li
                    key={role.roleId}
                    className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3.5 last:border-b-0 transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium tracking-tight">
                          {role.roleName ?? t("users.detail.untitledRole")}
                        </span>
                        {dirty && (
                          <span
                            className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]"
                            aria-label="modified"
                          />
                        )}
                      </div>
                      {role.description && (
                        <div className="mt-0.5 line-clamp-1 text-[12.5px] text-[var(--color-muted-foreground)]">
                          {role.description}
                        </div>
                      )}
                    </div>
                    <Switch
                      checked={isOn}
                      onCheckedChange={() => toggle(role)}
                      aria-label={t("users.detail.toggleRoleAria", { name: role.roleName ?? "" })}
                    />
                  </li>
                );
              })}
            </ul>
          )}
        </EntityDetailSection>
      </div>

      {/* Sessions */}
      {canViewSessions && (
        <SessionsCard
          sessions={sessionsQuery.data ?? []}
          isLoading={sessionsQuery.isLoading}
          isError={sessionsQuery.isError}
          error={sessionsQuery.error}
          canRevoke={canRevokeSessions}
          onRevoke={(id) => revokeOne.mutate(id)}
          onRevokeAll={() => setDialog({ mode: "revoke-all-sessions" })}
          revokingId={revokeOne.isPending ? revokeOne.variables : null}
          locale={locale}
        />
      )}

      {/* Change history */}
      <EntityDetailSection title={t("users.detail.changeHistory")}>
        <div className="px-5 py-4">
          <EntityAuditSection entityKey={userId} entityName="User" />
        </div>
      </EntityDetailSection>

      {/* Delete confirmation */}
      <Dialog
        open={dialog.mode === "delete"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("users.detail.deleteTitle")}</DialogTitle>
            <DialogDescription>
              {t("users.detail.deleteDesc", { name: display })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={removeUser.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => removeUser.mutate()}
              disabled={removeUser.isPending}
            >
              {removeUser.isPending ? t("common:feedback.deleting") : t("users.detail.deleteAction")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Toggle status confirmation */}
      <Dialog
        open={dialog.mode === "toggle-status"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {user.isActive ? t("users.deactivateConfirm", { name: display }) : t("users.activateConfirm", { name: display })}
            </DialogTitle>
            <DialogDescription>
              {user.isActive
                ? t("users.detail.deactivateDesc", { name: display })
                : t("users.detail.reactivateDesc", { name: display })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={toggleStatus.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button onClick={() => toggleStatus.mutate()} disabled={toggleStatus.isPending}>
              {toggleStatus.isPending
                ? t("users.detail.working")
                : user.isActive
                  ? t("common:actions.deactivate")
                  : t("common:actions.reactivate")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Impersonation confirmation */}
      <Dialog
        open={dialog.mode === "impersonate"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("users.detail.impersonateTitle", { name: display })}</DialogTitle>
            <DialogDescription>{t("users.detail.impersonateDesc")}</DialogDescription>
          </DialogHeader>
          <div className="px-6 pb-2">
            <label className="block">
              <span className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
                {t("users.detail.reasonLabel")}
              </span>
              <input
                type="text"
                value={impersonationReason}
                onChange={(e) => setImpersonationReason(e.target.value)}
                placeholder={t("users.detail.reasonPlaceholder")}
                maxLength={256}
                className={cn(
                  "mt-1.5 flex h-9 w-full rounded-md border border-[var(--color-input)]",
                  "bg-transparent px-3 py-1 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                )}
              />
            </label>
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={impersonate.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              onClick={() => impersonate.mutate()}
              disabled={impersonate.isPending}
              className={cn(
                "gap-1.5",
                "bg-[var(--color-warning)] text-[var(--color-warning-foreground,white)] hover:opacity-90",
              )}
            >
              <ShieldAlert className="h-3.5 w-3.5" />
              {impersonate.isPending ? t("users.detail.startingImpersonation") : t("users.detail.startImpersonation")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Revoke all sessions confirmation */}
      <Dialog
        open={dialog.mode === "revoke-all-sessions"}
        onOpenChange={(o) => (!o ? setDialog({ mode: "closed" }) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("users.detail.revokeAllTitle", { name: display })}</DialogTitle>
            <DialogDescription>{t("users.detail.revokeAllDesc")}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={revokeAll.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => revokeAll.mutate()}
              disabled={revokeAll.isPending}
            >
              {revokeAll.isPending ? t("users.detail.revoking") : t("users.detail.revokeAll")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ProfileRow({
  label,
  value,
}: {
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-baseline justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pb-2.5 last:border-b-0 last:pb-0">
      <div className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <div className="min-w-0 truncate text-right text-[13px] text-[var(--color-foreground)]">
        {value}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Sessions card
// ───────────────────────────────────────────────────────────────────────

function describeDevice(s: AdminUserSessionDto, unknownBrowser: string, unknownOs: string): string {
  const browser = s.browser ?? unknownBrowser;
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? unknownOs;
  return `${browser}${version} · ${os}`;
}

function deviceIcon(s: AdminUserSessionDto) {
  const isMobile = (s.deviceType ?? "").toLowerCase().includes("mobile");
  return isMobile ? Smartphone : MonitorSmartphone;
}

function SessionsCard({
  sessions,
  isLoading,
  isError,
  error,
  canRevoke,
  onRevoke,
  onRevokeAll,
  revokingId,
  locale,
}: {
  sessions: AdminUserSessionDto[];
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  canRevoke: boolean;
  onRevoke: (sessionId: string) => void;
  onRevokeAll: () => void;
  revokingId: string | null | undefined;
  locale: string;
}) {
  const { t } = useTranslation("identity");

  const sessionDateFmt = new Intl.DateTimeFormat(locale === "es" ? "es-CO" : "en-US", {
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

  const ordered = [...sessions].sort(
    (a, b) =>
      new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime(),
  );
  const activeCount = ordered.filter((s) => s.isActive).length;

  return (
    <EntityDetailSection
      title={t("users.detail.activeSessions")}
      icon={MonitorSmartphone}
      description={
        isLoading
          ? t("common:feedback.loading")
          : t("users.detail.activeSessionsDesc", { active: activeCount, total: ordered.length })
      }
      action={
        canRevoke && activeCount > 0 ? (
          <Button variant="outline" size="sm" onClick={onRevokeAll} className="gap-1.5">
            <XCircle className="h-3.5 w-3.5" /> {t("users.detail.revokeAll")}
          </Button>
        ) : undefined
      }
      padded={false}
    >
      {isLoading ? (
        <div className="space-y-3 p-5">
          <Skeleton className="h-14 w-full rounded-md" />
          <Skeleton className="h-14 w-full rounded-md" />
        </div>
      ) : isError ? (
        <div className="p-5">
          <ErrorBand message={describe(error)} />
        </div>
      ) : ordered.length === 0 ? (
        <div className="p-5 text-sm text-[var(--color-muted-foreground)]">
          {t("users.detail.noSessions")}
        </div>
      ) : (
        <ul>
          {ordered.map((session) => {
            const DIcon = deviceIcon(session);
            const isRevoking = revokingId === session.id;
            return (
              <li
                key={session.id}
                className={cn(
                  "flex items-center gap-3 border-b border-[var(--color-border)] px-5 py-3.5 last:border-b-0",
                  "transition-colors hover:bg-[var(--color-accent)]",
                  !session.isActive && "opacity-60",
                )}
              >
                <span
                  aria-hidden
                  className={cn(
                    "grid h-9 w-9 shrink-0 place-items-center rounded-lg",
                    session.isActive
                      ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                      : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
                  )}
                >
                  <DIcon className="h-4 w-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="truncate text-sm font-medium tracking-tight">
                      {describeDevice(session, t("users.detail.unknownBrowser"), t("users.detail.unknownOs"))}
                    </span>
                    {session.isActive ? (
                      <Badge variant="success">{t("common:status.active")}</Badge>
                    ) : (
                      <Badge variant="outline">{t("users.detail.ended")}</Badge>
                    )}
                  </div>
                  <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                    {session.ipAddress && (
                      <span className="inline-flex items-center gap-1 font-mono">
                        <Globe className="h-3 w-3" /> {session.ipAddress}
                      </span>
                    )}
                    <span className="inline-flex items-center gap-1">
                      <Clock className="h-3 w-3" /> {t("users.detail.lastSeen")}{" "}
                      {sessionDateFmt.format(new Date(session.lastActivityAt))}
                    </span>
                    <span className="opacity-70">
                      {t("users.detail.sessionStarted")} {sessionDateFmt.format(new Date(session.createdAt))}
                    </span>
                  </div>
                </div>
                {canRevoke && session.isActive && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onRevoke(session.id)}
                    disabled={isRevoking}
                    className="shrink-0 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"
                  >
                    <XCircle className="mr-1 h-3.5 w-3.5" />
                    {isRevoking ? t("users.detail.revoking") : t("users.detail.revoke")}
                  </Button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </EntityDetailSection>
  );
}

// Suppress unused-import warning for UsersIcon if we ever drop it; keep
// because tooltips and future actions may reuse it.
void UsersIcon;
