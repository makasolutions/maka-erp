import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  Check,
  ChevronDown,
  KeyRound,
  Lock,
  Minus,
  Search,
  ShieldCheck,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import {
  deleteRole,
  getPermissionsCatalog,
  getRoleWithPermissions,
  updateRolePermissions,
  upsertRole,
} from "@/api/identity";
import {
  groupPermissions,
  type PermissionDescriptor,
} from "@/api/permissions-catalog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
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
  EntityDetailSection,
  EntityDetailStat,
  ErrorBand,
  Field,
} from "@/components/list";
import { describe, pad2 } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

// System roles defined by the framework (RoleConstants.DefaultRoles on the
// server). These cannot be deleted, renamed, re-described, or have their
// permissions edited — the API rejects all four with 400/403, so we mirror
// those rules in the UI as a read-only mode rather than letting the user
// click a destructive action only to be turned away by a toast.
const SYSTEM_ROLE_NAMES: ReadonlyArray<string> = ["Admin", "Basic"];
const isSystemRoleName = (name?: string | null): boolean =>
  !!name && SYSTEM_ROLE_NAMES.includes(name);

export function RoleDetailPage() {
  const { t } = useTranslation("identity");
  const { roleId = "" } = useParams<{ roleId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const roleQuery = useQuery({
    queryKey: ["identity", "roles", roleId],
    queryFn: () => getRoleWithPermissions(roleId),
    enabled: !!roleId,
  });

  // Permission catalog — fetched from the server so the editor knows about
  // every module's permissions, not just Identity's. Stable for the session;
  // tenant context only switches on full sign-in/out which kills the cache.
  const catalogQuery = useQuery({
    queryKey: ["identity", "permissions", "catalog"],
    queryFn: getPermissionsCatalog,
    staleTime: 10 * 60 * 1000,
  });
  const catalog = useMemo<PermissionDescriptor[]>(
    () => catalogQuery.data ?? [],
    [catalogQuery.data],
  );
  const catalogGroups = useMemo(() => groupPermissions(catalog), [catalog]);

  const role = roleQuery.data;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [initial, setInitial] = useState<Set<string>>(new Set());
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Permissions editor — browse-friendly state. Search, filter chips, and
  // a Set of explicitly-expanded groups. Search or a non-"all" filter
  // implicitly expands every matching group so results never hide behind
  // a closed accordion.
  type PermFilter = "all" | "enabled" | "modified" | "basic";
  const [searchQuery, setSearchQuery] = useState("");
  const [filter, setFilter] = useState<PermFilter>("all");
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const searchInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!role) return;
    setName(role.name);
    setDescription(role.description ?? "");
    const next = new Set(role.permissions ?? []);
    setSelected(next);
    setInitial(new Set(next));
  }, [role]);

  const dirtyMeta = useMemo(() => {
    if (!role) return false;
    return name.trim() !== role.name || (description ?? "") !== (role.description ?? "");
  }, [role, name, description]);

  const dirtyPerms = useMemo(() => {
    if (selected.size !== initial.size) return true;
    for (const p of selected) if (!initial.has(p)) return true;
    return false;
  }, [selected, initial]);

  const isDirty = dirtyMeta || dirtyPerms;

  const togglePerm = (n: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(n)) next.delete(n);
      else next.add(n);
      return next;
    });
  };

  const setGroupAll = (perms: PermissionDescriptor[], on: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev);
      for (const p of perms) {
        if (on) next.add(p.name);
        else next.delete(p.name);
      }
      return next;
    });
  };

  const presetBasic = () =>
    setSelected(new Set(catalog.filter((p) => p.isBasic).map((p) => p.name)));
  const presetAll = () => setSelected(new Set(catalog.map((p) => p.name)));
  const presetClear = () => setSelected(new Set());

  // ── Editor filter pipeline ──────────────────────────────────────────────
  const modifiedCount = useMemo(() => {
    let n = 0;
    for (const p of catalog) {
      if (selected.has(p.name) !== initial.has(p.name)) n += 1;
    }
    return n;
  }, [selected, initial, catalog]);
  const basicCount = useMemo(
    () => catalog.filter((p) => p.isBasic).length,
    [catalog],
  );

  const matchesSearch = (p: PermissionDescriptor, q: string) => {
    if (!q) return true;
    const needle = q.toLowerCase();
    return (
      p.resource.toLowerCase().includes(needle) ||
      p.action.toLowerCase().includes(needle) ||
      p.description.toLowerCase().includes(needle) ||
      p.name.toLowerCase().includes(needle)
    );
  };
  const matchesFilter = (p: PermissionDescriptor): boolean => {
    switch (filter) {
      case "enabled":
        return selected.has(p.name);
      case "modified":
        return selected.has(p.name) !== initial.has(p.name);
      case "basic":
        return !!p.isBasic;
      default:
        return true;
    }
  };

  // Visible groups: each group keeps only the perms that pass both filters.
  const visibleGroups = useMemo(() => {
    const q = searchQuery.trim();
    return catalogGroups.map((g) => ({
      resource: g.resource,
      permissions: g.permissions.filter((p) => matchesSearch(p, q) && matchesFilter(p)),
    })).filter((g) => g.permissions.length > 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchQuery, filter, selected, initial, catalogGroups]);

  // Force-expand when a filter or search is active so results aren't hiding.
  const forceExpand = searchQuery.trim() !== "" || filter !== "all";

  const toggleGroup = (resource: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(resource)) next.delete(resource);
      else next.add(resource);
      return next;
    });
  };
  const expandAllGroups = () =>
    setExpandedGroups(new Set(catalogGroups.map((g) => g.resource)));
  const collapseAllGroups = () => setExpandedGroups(new Set());

  const saveMeta = useMutation({
    mutationFn: () =>
      upsertRole({
        id: roleId,
        name: name.trim(),
        description: description.trim() || undefined,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles", roleId] });
    },
    onError: (err) => toast.error(t("roles.detail.updateFailed"), { description: describe(err) }),
  });

  const savePerms = useMutation({
    mutationFn: () => updateRolePermissions(roleId, Array.from(selected)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles", roleId] });
    },
    onError: (err) => toast.error(t("roles.detail.permissionsUpdateFailed"), { description: describe(err) }),
  });

  const removeRole = useMutation({
    mutationFn: () => deleteRole(roleId),
    onSuccess: () => {
      toast.success(t("roles.detail.deleted"));
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      navigate("/identity/roles");
    },
    onError: (err) => {
      toast.error(t("roles.detail.deleteFailed"), { description: describe(err) });
      setConfirmDelete(false);
    },
  });

  const saveAll = async () => {
    try {
      if (dirtyMeta) await saveMeta.mutateAsync();
      if (dirtyPerms) await savePerms.mutateAsync();
      toast.success(t("roles.saved"));
    } catch {
      // mutations report their own errors via toast
    }
  };

  const reset = () => {
    if (!role) return;
    setName(role.name);
    setDescription(role.description ?? "");
    setSelected(new Set(role.permissions ?? []));
  };

  const isSaving = saveMeta.isPending || savePerms.isPending;

  // Block on both queries so the permissions editor never renders with an
  // empty catalog (which would look broken: zero groups, 0/0 enabled, etc.).
  if (roleQuery.isLoading || catalogQuery.isLoading) {
    return (
      <div className="space-y-6">
        <EntityDetailBack to="/identity/roles" label={t("roles.detail.backToRoles")} />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-96 rounded-xl" />
      </div>
    );
  }

  if (roleQuery.isError || !role) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/roles" label={t("roles.detail.backToRoles")} />
        <ErrorBand message={roleQuery.error ? describe(roleQuery.error) : t("roles.detail.notFound")} />
      </div>
    );
  }

  if (catalogQuery.isError) {
    return (
      <div className="space-y-4">
        <EntityDetailBack to="/identity/roles" label={t("roles.detail.backToRoles")} />
        <ErrorBand
          message={t("roles.detail.catalogError", { error: describe(catalogQuery.error) })}
        />
      </div>
    );
  }

  const totalSelected = selected.size;
  const totalCatalog = catalog.length;
  const isSystem = isSystemRoleName(role.name);

  return (
    <div className="space-y-5 pb-12">
      <EntityDetailBack to="/identity/roles" label={t("roles.detail.backToRoles")} />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={role.name} icon={ShieldCheck} />}
        title={role.name}
        badges={
          <>
            {isSystem && (
              <Badge variant="outline">
                <Lock className="h-3 w-3" /> {t("roles.systemBadge")}
              </Badge>
            )}
          </>
        }
        subtitle={role.description || (isSystem ? t("roles.detail.systemSubtitle") : t("roles.detail.customSubtitle"))}
        actions={
          <Button
            variant="destructive"
            size="sm"
            onClick={() => setConfirmDelete(true)}
            disabled={isSystem}
            title={isSystem ? t("roles.detail.systemCannotDelete") : undefined}
          >
            <Trash2 className="mr-1 h-3.5 w-3.5" /> {t("roles.detail.deleteTitle")}
          </Button>
        }
        stats={
          <>
            <EntityDetailStat
              icon={KeyRound}
              value={`${pad2(totalSelected)} / ${pad2(totalCatalog)}`}
              label={t("roles.detail.permissionsLabel")}
              tone="primary"
            />
          </>
        }
      />

      {isSystem && (
        <div
          role="status"
          aria-live="polite"
          className="flex items-start gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3"
        >
          <span
            aria-hidden
            className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
          >
            <Lock className="h-3.5 w-3.5" />
          </span>
          <div className="min-w-0 text-sm leading-relaxed">
            <p className="font-medium text-[var(--color-foreground)]">
              {t("roles.detail.systemRoleReadOnly")}
            </p>
            <p className="mt-0.5 text-[12.5px] text-[var(--color-muted-foreground)]">
              {t("roles.detail.systemRoleDesc", { name: role.name })}
            </p>
          </div>
        </div>
      )}

      {/* Metadata */}
      <EntityDetailSection
        title={t("roles.detail.detailsTitle")}
        icon={ShieldCheck}
        description={t("roles.detail.detailsDesc")}
      >
        <div className="grid gap-4 md:grid-cols-2">
          <Field id="role-name" label={t("roles.fields.name")} required>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={128}
              readOnly={isSystem}
              aria-readonly={isSystem || undefined}
              className={cn(isSystem && "cursor-not-allowed opacity-70")}
            />
          </Field>
          <Field id="role-desc" label={t("roles.fields.description")}>
            <Input
              id="role-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={t("roles.detail.descPlaceholder")}
              maxLength={512}
              readOnly={isSystem}
              aria-readonly={isSystem || undefined}
              className={cn(isSystem && "cursor-not-allowed opacity-70")}
            />
          </Field>
        </div>
      </EntityDetailSection>

      {/* Permission editor */}
      <EntityDetailSection
        title={t("roles.detail.permissionsTitle")}
        icon={KeyRound}
        description={t("roles.detail.permissionsDesc")}
        action={
          <div className="flex flex-wrap gap-1.5">
            <PresetButton
              onClick={presetBasic}
              icon={<Sparkles className="h-3 w-3" />}
              label={t("roles.detail.presetBasic")}
              disabled={isSystem}
            />
            <PresetButton onClick={presetAll} label={t("roles.detail.filterAll")} disabled={isSystem} />
            <PresetButton onClick={presetClear} label={t("roles.detail.presetClear")} disabled={isSystem} />
          </div>
        }
        padded={false}
        footer={
          !isSystem ? (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
                {isDirty ? (
                  <span className="inline-flex items-center gap-1.5 text-[var(--color-warning)]">
                    <span className="inline-block h-1.5 w-1.5 rounded-full bg-[var(--color-warning)]" />
                    {t("roles.detail.unsavedChanges")}
                  </span>
                ) : (
                  t("roles.detail.allChangesSaved")
                )}
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={reset} disabled={!isDirty || isSaving}>
                  {t("common:actions.discard")}
                </Button>
                <Button
                  size="sm"
                  onClick={saveAll}
                  disabled={!isDirty || isSaving}
                >
                  {isSaving ? t("common:feedback.saving") : t("common:actions.saveChanges")}
                </Button>
              </div>
            </div>
          ) : undefined
        }
      >
        {/* Toolbar — search + filter chips */}
        <div className="border-b border-[var(--color-border)] px-5 py-3">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div className="relative min-w-0 flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
              <input
                ref={searchInputRef}
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t("roles.detail.searchPlaceholder")}
                aria-label={t("roles.detail.searchAriaLabel")}
                className={cn(
                  "h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-9",
                  "text-[13px] outline-none transition-colors",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                )}
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => {
                    setSearchQuery("");
                    searchInputRef.current?.focus();
                  }}
                  aria-label={t("roles.clearSearch")}
                  className="absolute right-2 top-1/2 grid size-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                >
                  <X className="size-3" />
                </button>
              )}
            </div>
            <div className="flex items-center gap-1 overflow-x-auto">
              <FilterChip active={filter === "all"} count={totalCatalog} onClick={() => setFilter("all")}>
                {t("roles.detail.filterAll")}
              </FilterChip>
              <FilterChip
                active={filter === "enabled"}
                count={totalSelected}
                onClick={() => setFilter("enabled")}
                disabled={totalSelected === 0}
              >
                {t("roles.detail.filterEnabled")}
              </FilterChip>
              <FilterChip
                active={filter === "modified"}
                count={modifiedCount}
                tone="warning"
                onClick={() => setFilter("modified")}
                disabled={modifiedCount === 0}
              >
                {t("roles.detail.filterModified")}
              </FilterChip>
              <FilterChip
                active={filter === "basic"}
                count={basicCount}
                onClick={() => setFilter("basic")}
              >
                {t("roles.detail.filterBasic")}
              </FilterChip>
            </div>
          </div>
        </div>

        {/* Summary strip — running count + expand/collapse-all */}
        <div className="flex items-center justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] bg-[var(--color-secondary)] px-5 py-2">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[11.5px]">
            <span className="font-mono font-semibold tabular-nums text-[var(--color-foreground)]">
              {pad2(totalSelected)} / {pad2(totalCatalog)}
            </span>
            <span className="text-[var(--color-muted-foreground)]">{t("roles.detail.enabledLabel")}</span>
            {modifiedCount > 0 && (
              <span className="inline-flex items-center gap-1 text-[var(--color-warning)]">
                <span aria-hidden className="text-[var(--color-muted-foreground)]">·</span>
                <span aria-hidden className="inline-block size-1.5 rounded-full bg-[var(--color-warning)]" />
                {modifiedCount} {t("roles.detail.modifiedLabel")}
              </span>
            )}
            {forceExpand && visibleGroups.length > 0 && (
              <span className="text-[var(--color-muted-foreground)]">
                <span aria-hidden className="mr-1">·</span>
                {t("roles.detail.matchCount", { count: visibleGroups.reduce((n, g) => n + g.permissions.length, 0) })}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2 text-[11px]">
            <button
              type="button"
              onClick={expandAllGroups}
              disabled={forceExpand}
              className={cn(
                "cursor-pointer font-medium uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]",
                forceExpand && "cursor-not-allowed opacity-40 hover:text-[var(--color-muted-foreground)]",
              )}
            >
              {t("roles.detail.expandAll")}
            </button>
            <span aria-hidden className="text-[var(--color-border-strong)]">·</span>
            <button
              type="button"
              onClick={collapseAllGroups}
              disabled={forceExpand}
              className={cn(
                "cursor-pointer font-medium uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]",
                forceExpand && "cursor-not-allowed opacity-40 hover:text-[var(--color-muted-foreground)]",
              )}
            >
              {t("roles.detail.collapseAll")}
            </button>
          </div>
        </div>

        {/* Accordion of resource groups */}
        {visibleGroups.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 px-5 py-16 text-center">
            <span
              aria-hidden
              className="grid size-10 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
            >
              <Search className="size-4" />
            </span>
            <div>
              <p className="text-[13px] font-medium text-[var(--color-foreground)]">
                {t("roles.detail.noPermissionsMatch")}
              </p>
              <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                {t("roles.detail.noPermissionsMatchDesc")}
              </p>
            </div>
            {(searchQuery || filter !== "all") && (
              <button
                type="button"
                onClick={() => {
                  setSearchQuery("");
                  setFilter("all");
                  searchInputRef.current?.focus();
                }}
                className={cn(
                  "mt-1 inline-flex h-7 cursor-pointer items-center gap-1 rounded-full px-3 text-[11px] font-medium",
                  "bg-[var(--color-card)] ring-1 ring-inset ring-[var(--color-border)]",
                  "text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                )}
              >
                <X className="size-3" /> {t("roles.detail.resetFilters")}
              </button>
            )}
          </div>
        ) : (
          <div className="divide-y divide-[var(--color-border)]">
            {visibleGroups.map((group) => {
              const fullGroup = catalogGroups.find((g) => g.resource === group.resource)!;
              const onCount = fullGroup.permissions.filter((p) => selected.has(p.name)).length;
              const total = fullGroup.permissions.length;
              const allOn = onCount === total;
              const someOn = onCount > 0 && !allOn;
              const groupModified = fullGroup.permissions.filter(
                (p) => selected.has(p.name) !== initial.has(p.name),
              ).length;
              const isExpanded = forceExpand || expandedGroups.has(group.resource);
              const visibleCount = group.permissions.length;
              return (
                <PermissionGroupCard
                  key={group.resource}
                  resource={group.resource}
                  visiblePermissions={group.permissions}
                  totalInGroup={total}
                  onCount={onCount}
                  allOn={allOn}
                  someOn={someOn}
                  groupModified={groupModified}
                  isExpanded={isExpanded}
                  showingPartial={visibleCount !== total}
                  visibleCount={visibleCount}
                  onToggleExpand={() => toggleGroup(group.resource)}
                  onSetGroupAll={(on) => setGroupAll(fullGroup.permissions, on)}
                  onTogglePerm={togglePerm}
                  selected={selected}
                  initial={initial}
                  disabled={isSystem}
                />
              );
            })}
          </div>
        )}
      </EntityDetailSection>

      {/* Delete dialog */}
      <Dialog
        open={confirmDelete}
        onOpenChange={(o) => (!o ? setConfirmDelete(false) : undefined)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("roles.detail.deleteTitle")}</DialogTitle>
            <DialogDescription>
              {t("roles.detail.deleteDesc", { name: role.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={removeRole.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              variant="destructive"
              onClick={() => removeRole.mutate()}
              disabled={removeRole.isPending}
            >
              {removeRole.isPending ? t("common:feedback.deleting") : t("roles.detail.deleteTitle")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function PresetButton({
  onClick,
  label,
  icon,
  disabled,
}: {
  onClick: () => void;
  label: string;
  icon?: React.ReactNode;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={cn(
        "inline-flex h-7 items-center gap-1 rounded-full bg-[var(--color-card)] px-3",
        "ring-1 ring-inset ring-[var(--color-border)]",
        "text-[11px] font-medium text-[var(--color-muted-foreground)]",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        disabled && "cursor-not-allowed opacity-50 hover:bg-[var(--color-card)] hover:text-[var(--color-muted-foreground)]",
      )}
    >
      {icon}
      {label}
    </button>
  );
}

/**
 * FilterChip — pill that toggles a permissions filter. Shows a live count
 * on the right. `tone="warning"` paints the count amber for the Modified
 * chip so unsaved changes are scannable without reading the label.
 */
function FilterChip({
  active,
  count,
  onClick,
  disabled,
  tone,
  children,
}: {
  active: boolean;
  count: number;
  onClick: () => void;
  disabled?: boolean;
  tone?: "warning";
  children: React.ReactNode;
}) {
  const isWarning = tone === "warning" && count > 0;
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      className={cn(
        "inline-flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-full px-2.5 text-[11.5px] font-medium",
        "transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        disabled && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
      )}
    >
      <span>{children}</span>
      <span
        className={cn(
          "rounded-full px-1.5 py-0.5 text-[10px] font-semibold tabular-nums",
          active
            ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] text-[var(--color-primary)]"
            : isWarning
              ? "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] text-[var(--color-warning)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        )}
      >
        {count}
      </span>
    </button>
  );
}

/**
 * PermissionGroupCard — collapsible accordion row for one resource.
 */
function PermissionGroupCard({
  resource,
  visiblePermissions,
  totalInGroup,
  onCount,
  allOn,
  someOn,
  groupModified,
  isExpanded,
  showingPartial,
  visibleCount,
  onToggleExpand,
  onSetGroupAll,
  onTogglePerm,
  selected,
  initial,
  disabled,
}: {
  resource: string;
  visiblePermissions: PermissionDescriptor[];
  totalInGroup: number;
  onCount: number;
  allOn: boolean;
  someOn: boolean;
  groupModified: number;
  isExpanded: boolean;
  showingPartial: boolean;
  visibleCount: number;
  onToggleExpand: () => void;
  onSetGroupAll: (on: boolean) => void;
  onTogglePerm: (name: string) => void;
  selected: Set<string>;
  initial: Set<string>;
  disabled?: boolean;
}) {
  const { t } = useTranslation("identity");
  return (
    <div>
      <div
        role="button"
        tabIndex={0}
        onClick={onToggleExpand}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onToggleExpand();
          }
        }}
        aria-expanded={isExpanded}
        className={cn(
          "group/grouphdr flex w-full cursor-pointer items-center gap-3 px-5 py-3.5 text-left",
          "transition-colors duration-[var(--duration-fast)]",
          "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.03)]",
          isExpanded && "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.02)]",
        )}
      >
        {/* Group tri-state checkbox */}
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onSetGroupAll(!allOn);
          }}
          disabled={disabled}
          aria-label={t("roles.detail.toggleAllAria", { resource })}
          className={cn(
            "grid size-5 shrink-0 cursor-pointer place-items-center rounded border transition-colors",
            allOn
              ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
              : someOn
                ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.40)] text-[var(--color-primary-foreground)]"
                : "border-[var(--color-input)] hover:border-[var(--color-foreground)]/40",
            disabled && "cursor-not-allowed opacity-60",
          )}
        >
          {allOn ? <Check className="size-3.5" /> : someOn ? <Minus className="size-3.5" /> : null}
        </button>

        {/* Resource name + meta */}
        <div className="flex min-w-0 flex-1 items-baseline gap-3">
          <span className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
            {resource}
          </span>
          <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
            {pad2(onCount)} / {pad2(totalInGroup)}
          </span>
          {/* Mini pip bar */}
          <PipBar onCount={onCount} total={totalInGroup} />
          {groupModified > 0 && (
            <span
              className="inline-flex items-center gap-1 text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-warning)]"
              title={t("roles.detail.unsavedChangeCount", { count: groupModified })}
            >
              <span aria-hidden className="size-1.5 rounded-full bg-[var(--color-warning)]" />
              {groupModified} {t("roles.detail.changedLabel")}
            </span>
          )}
          {showingPartial && (
            <span className="text-[10.5px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
              · {t("roles.detail.matchCount", { count: visibleCount })}
            </span>
          )}
        </div>

        {/* All / none chip actions */}
        <div className="hidden items-center gap-1 sm:flex">
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onSetGroupAll(true);
            }}
            disabled={disabled || allOn}
            className={cn(
              "cursor-pointer rounded-full px-2 py-0.5 text-[10.5px] font-medium uppercase tracking-wider",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)]",
              (disabled || allOn) && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
            )}
          >
            {t("roles.detail.all")}
          </button>
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onSetGroupAll(false);
            }}
            disabled={disabled || onCount === 0}
            className={cn(
              "cursor-pointer rounded-full px-2 py-0.5 text-[10.5px] font-medium uppercase tracking-wider",
              "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "transition-colors duration-[var(--duration-fast)]",
              (disabled || onCount === 0) && "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
            )}
          >
            {t("roles.detail.none")}
          </button>
        </div>

        {/* Chevron */}
        <ChevronDown
          aria-hidden
          className={cn(
            "size-4 shrink-0 text-[var(--color-muted-foreground)] transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            isExpanded && "rotate-180",
          )}
        />
      </div>

      {/* Body — list of horizontal permission rows */}
      {isExpanded && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {visiblePermissions.map((perm) => {
            const checked = selected.has(perm.name);
            const dirty = checked !== initial.has(perm.name);
            return (
              <PermissionRow
                key={perm.name}
                perm={perm}
                checked={checked}
                dirty={dirty}
                onToggle={() => onTogglePerm(perm.name)}
                disabled={disabled}
              />
            );
          })}
        </div>
      )}
    </div>
  );
}

/**
 * PipBar — n discrete pips, filled left-to-right.
 */
function PipBar({ onCount, total }: { onCount: number; total: number }) {
  const cap = Math.min(total, 12);
  const filledVisible = Math.round((onCount / total) * cap);
  return (
    <span aria-hidden className="flex items-center gap-[3px]">
      {Array.from({ length: cap }).map((_, i) => (
        <span
          key={i}
          className={cn(
            "h-2 w-[3px] rounded-[1px] transition-colors",
            i < filledVisible
              ? "bg-[var(--color-primary)]"
              : "bg-[var(--color-border-strong)]",
          )}
        />
      ))}
    </span>
  );
}

/**
 * PermissionRow — one permission as a horizontal scan-line.
 */
function PermissionRow({
  perm,
  checked,
  dirty,
  onToggle,
  disabled,
}: {
  perm: PermissionDescriptor;
  checked: boolean;
  dirty: boolean;
  onToggle: () => void;
  disabled?: boolean;
}) {
  const { t } = useTranslation("identity");
  return (
    <label
      className={cn(
        "group/row relative flex items-center gap-3 px-5 py-2.5 pl-[3.25rem] text-[12.5px]",
        "transition-colors duration-[var(--duration-fast)]",
        disabled ? "cursor-not-allowed" : "cursor-pointer",
        checked
          ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)] hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.07)]"
          : !disabled
            ? "hover:bg-[var(--color-accent)]"
            : "",
      )}
    >
      <input
        type="checkbox"
        className="sr-only"
        checked={checked}
        onChange={onToggle}
        disabled={disabled}
      />
      {/* Custom checkbox */}
      <span
        aria-hidden
        className={cn(
          "grid size-4 shrink-0 place-items-center rounded border transition-all",
          checked
            ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
            : "border-[var(--color-input)] bg-transparent group-hover/row:border-[var(--color-foreground)]/40",
        )}
      >
        {checked && <Check className="size-3" />}
      </span>

      {/* Action handle (mono) */}
      <span
        className={cn(
          "min-w-[110px] shrink-0 truncate font-mono text-[12px] tabular-nums",
          checked ? "text-[var(--color-primary)]" : "text-[var(--color-foreground)]",
        )}
      >
        {perm.action}
      </span>

      {/* Human description */}
      <span
        className={cn(
          "min-w-0 flex-1 truncate",
          checked
            ? "text-[var(--color-foreground)]"
            : "text-[var(--color-muted-foreground)]",
        )}
      >
        {perm.description}
      </span>

      {/* Badges — these are technical tags, kept as-is */}
      <span className="flex shrink-0 items-center gap-1.5">
        {perm.isRoot && (
          <span
            className={cn(
              "rounded-full px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em]",
              "bg-[oklch(from_var(--color-saffron)_l_c_h_/_0.16)] text-[var(--color-saffron)]",
            )}
            title={t("roles.rootPermissionTitle")}
          >
            root
          </span>
        )}
        {perm.isBasic && (
          <span
            className={cn(
              "rounded-full px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-[0.12em]",
              "bg-[oklch(from_var(--color-info)_l_c_h_/_0.16)] text-[var(--color-info)]",
            )}
          >
            basic
          </span>
        )}
        {dirty && (
          <span
            aria-label="modified"
            className="inline-block size-1.5 rounded-full bg-[var(--color-warning)]"
          />
        )}
      </span>
    </label>
  );
}
