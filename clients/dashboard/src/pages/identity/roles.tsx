import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Plus, Shield } from "lucide-react";
import { toast } from "sonner";
import { listRoles, upsertRole, type RoleDto } from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const SYSTEM_ROLE_NAMES = new Set(["admin", "administrator", "basic", "user"]);

// Desktop grid template, shared by header + rows + skeleton.
const DESKTOP_COLS =
  "grid-cols-[1fr_120px_24px] lg:grid-cols-[1.4fr_2fr_120px_24px]";

function newGuid(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return "00000000-0000-0000-0000-000000000000".replace(/0/g, () =>
    Math.floor(Math.random() * 16).toString(16),
  );
}

export function RolesPage() {
  const { t } = useTranslation("identity");
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search.trim().toLowerCase()), 200);
    return () => clearTimeout(timer);
  }, [search]);

  const query = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
  });

  const roles = query.data ?? [];

  const filtered = useMemo(() => {
    if (!debounced) return roles;
    return roles.filter(
      (r) =>
        r.name.toLowerCase().includes(debounced) ||
        (r.description ?? "").toLowerCase().includes(debounced),
    );
  }, [roles, debounced]);

  const searchActive = debounced.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Shield}
        title={t("roles.title")}
        total={query.data ? roles.length : null}
        unit={t("roles.singular")}
        unitPlural={t("roles.plural")}
        description={t("roles.description")}
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("roles.actions.create")}
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder={t("roles.searchPlaceholder")}
      />

      {query.isLoading ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : filtered.length === 0 ? (
        <EntityEmpty
          icon={Shield}
          title={searchActive ? t("roles.emptySearch") : t("roles.empty.title")}
          body={
            searchActive
              ? t("roles.emptySearchBody", { term: debounced })
              : t("roles.empty.body")
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => setSearch("")}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                {t("roles.clearSearch")}
              </Button>
            ) : (
              <Button
                onClick={() => setCreateOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                {t("roles.addRole")}
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("roles.rolesFound", { count: filtered.length })}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {filtered.map((role) => (
              <RoleMobileCard key={role.id} role={role} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>{t("roles.columns.name")}</span>
              <span className="hidden lg:block">{t("roles.columns.description")}</span>
              <span>{t("roles.fields.permissions")}</span>
              <span />
            </EntityListHeader>

            {filtered.map((role, i) => (
              <RoleDesktopRow
                key={role.id}
                role={role}
                isLast={i === filtered.length - 1}
              />
            ))}
          </EntityListCard>
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <CreateRoleDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Rows
// ───────────────────────────────────────────────────────────────────────

function RoleMobileCard({ role }: { role: RoleDto }) {
  const { t } = useTranslation("identity");
  const isSystem = SYSTEM_ROLE_NAMES.has(role.name.toLowerCase());
  const n = role.permissions?.length ?? 0;
  const permLabel = role.permissions == null ? "—" : t("roles.permissionCount", { count: n });

  return (
    <EntityMobileCard
      href={`/identity/roles/${role.id}`}
      aria-label={t("roles.openRoleAria", { name: role.name })}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={role.name} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {role.name}
              </p>
              {isSystem && <EntityStatusBadge>{t("roles.systemBadge")}</EntityStatusBadge>}
            </div>
            <p
              className={cn(
                "mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]",
                !role.description && "italic opacity-60",
              )}
            >
              {role.description ?? t("roles.noDescriptionOnFile")}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-1.5">
        <EntityStatusBadge tone="info">{permLabel}</EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function RoleDesktopRow({ role, isLast }: { role: RoleDto; isLast: boolean }) {
  const { t } = useTranslation("identity");
  const isSystem = SYSTEM_ROLE_NAMES.has(role.name.toLowerCase());
  const n = role.permissions?.length ?? 0;
  const permLabel = role.permissions == null ? "—" : t("roles.permissionCount", { count: n });

  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast}>
      {/* Name */}
      <Link
        to={`/identity/roles/${role.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={role.name} size={36} />
        <span className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {role.name}
        </span>
        {isSystem && (
          <EntityStatusBadge className="shrink-0">{t("roles.systemBadge")}</EntityStatusBadge>
        )}
      </Link>

      {/* Description (lg+) */}
      <div className="hidden lg:block">
        <p
          className={cn(
            "truncate text-[12.5px] text-[var(--color-muted-foreground)]",
            !role.description && "italic opacity-60",
          )}
          title={role.description ?? undefined}
        >
          {role.description ?? t("roles.noDescriptionOnFile")}
        </p>
      </div>

      {/* Permission count */}
      <span className="font-mono text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {permLabel}
      </span>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateRoleDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation("identity");
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  useEffect(() => {
    if (!open) {
      setName("");
      setDescription("");
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      upsertRole({
        id: newGuid(),
        name: name.trim(),
        description: description.trim() || undefined,
      }),
    onSuccess: (role) => {
      toast.success(t("roles.roleCreated"), {
        description: t("roles.roleCreatedDesc", { name: role.name }),
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "roles"] });
      onClose();
      navigate(`/identity/roles/${role.id}`);
    },
    onError: (err) =>
      toast.error(t("roles.createFailed"), { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim()) return;
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("roles.createTitle")}</DialogTitle>
            <DialogDescription>{t("roles.createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="role-name" label={t("roles.fields.name")} required>
              <Input
                id="role-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={t("roles.namePlaceholder")}
                required
                autoFocus
                maxLength={128}
              />
            </Field>
            <Field
              id="role-description"
              label={t("roles.fields.description")}
              hint={t("roles.descriptionHint")}
            >
              <Input
                id="role-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("roles.descriptionPlaceholder")}
                maxLength={512}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button
                type="button"
                variant="outline"
                disabled={mutation.isPending}
              >
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !name.trim()}
              className="gap-1.5"
            >
              <Plus className="h-4 w-4" />
              {mutation.isPending ? t("roles.creating") : t("roles.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
