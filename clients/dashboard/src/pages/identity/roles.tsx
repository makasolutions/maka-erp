import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, Plus, Shield } from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { toast } from "sonner";
import { listRoles, upsertRole } from "@/api/identity";
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
  EntityPageHeader,
  EntityStatusBadge,
  Field,
  FormActions,
  FormGrid,
} from "@/components/list";
import { SaveIcon, CancelIcon } from "@/components/ui/icons";
import { rules, validateSchema } from "@/lib/validation/rules";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import { describe } from "@/lib/list-helpers";
import { P } from "@/auth/permissions";

const SYSTEM_ROLE_NAMES = new Set(["admin", "administrator", "basic", "user"]);

type RoleRow = {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  systemLabel: string;
  permLabel: string;
};

// ── Cell templates (hook-free; read enriched row fields) ──────────────────
function RoleNameCell(row: RoleRow) {
  return (
    <div className="flex min-w-0 items-center gap-2">
      <span className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</span>
      {row.isSystem && <EntityStatusBadge>{row.systemLabel}</EntityStatusBadge>}
    </div>
  );
}
function RolePermsCell(row: RoleRow) {
  return <span className="font-mono text-[12px] tabular-nums text-[var(--color-muted-foreground)]">{row.permLabel}</span>;
}

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
  const { t: tc } = useTranslation("common");
  const navigate = useNavigate();
  const [createOpen, setCreateOpen] = useState(false);
  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");

  const query = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
  });

  const allRoles = query.data ?? [];

  const rows: RoleRow[] = useMemo(() => {
    const q = search.trim().toLowerCase();
    return allRoles
      .filter(
        (r) =>
          !q ||
          r.name.toLowerCase().includes(q) ||
          (r.description ?? "").toLowerCase().includes(q),
      )
      .map((r) => ({
        id: r.id,
        name: r.name,
        description: r.description ?? "",
        isSystem: SYSTEM_ROLE_NAMES.has(r.name.toLowerCase()),
        systemLabel: t("roles.systemBadge"),
        permLabel:
          r.permissions == null
            ? "—"
            : t("roles.permissionCount", { count: r.permissions.length }),
      }));
  }, [allRoles, search, t]);

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("roles.columns.name"), template: RoleNameCell as any, minWidth: 220 },
      { field: "description", headerText: t("roles.columns.description"), minWidth: 280 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "permLabel", headerText: t("roles.fields.permissions"), template: RolePermsCell as any, width: 150, allowSorting: false },
    ],
    [t],
  );

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Shield}
        title={t("roles.title")}
        total={query.data ? allRoles.length : null}
        unit={t("roles.singular")}
        unitPlural={t("roles.plural")}
        description={t("roles.description")}
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
          perm={P.identity.roles.create}
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("roles.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={() => setSearch("")}
        filters={
          <MakaFilterField label={tc("gridFilters.search")} className="grow">
            <MakaFilterInput
              value={search}
              onChange={setSearch}
              placeholder={t("roles.searchPlaceholder")}
              ariaLabel={tc("gridFilters.search")}
              className="min-w-64"
            />
          </MakaFilterField>
        }
      />

      <MakaGridClient<RoleRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="roles"
        entityName={t("roles.singular")}
        onRowClick={(row) => navigate(`/identity/roles/${row.id}`)}
        onClearFilters={() => setSearch("")}
      />

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
  const { t: tcommon } = useTranslation("common");
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [showErrors, setShowErrors] = useState(false);
  const schema = { name: [rules.required(), rules.min(2), rules.max(128)], description: [rules.max(512)] };
  const errs = showErrors ? validateSchema({ name, description }, schema, tcommon) : {} as Record<string, string>;

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
    if (Object.keys(validateSchema({ name, description }, schema, tcommon)).length > 0) { setShowErrors(true); return; }
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("roles.createTitle")}</DialogTitle>
            <DialogDescription>{t("roles.createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="role-name" span={4} label={t("roles.fields.name")} required error={errs.name}>
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
                span={8}
                label={t("roles.fields.description")}
                error={errs.description}
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
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <FormActions
              secondary={
                <DialogClose asChild>
                  <Button type="button" variant="outline" disabled={mutation.isPending}>
                    <CancelIcon className="size-4" />{tcommon("actions.cancel")}
                  </Button>
                </DialogClose>
              }
              primary={
                <Button type="submit" disabled={mutation.isPending} className="gap-1.5">
                  <SaveIcon className="size-4" />
                  {mutation.isPending ? t("roles.creating") : t("roles.create")}
                </Button>
              }
            />
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
