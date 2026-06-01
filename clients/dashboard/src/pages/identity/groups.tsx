import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, Plus, Star, UsersRound } from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { toast } from "sonner";
import {
  createGroup,
  listGroups,
} from "@/api/identity";
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
import { Switch } from "@/components/ui/switch";
import {
  EntityPageHeader,
  EntityStatusBadge,
  Field,
  FormGrid,
} from "@/components/list";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import { describe } from "@/lib/list-helpers";
import { P } from "@/auth/permissions";

type GroupRow = {
  id: string;
  name: string;
  description: string;
  membersLabel: string;
  rolesLabel: string;
  isDefault: boolean;
  isSystemGroup: boolean;
  defaultLabel: string;
  systemLabel: string;
};

// ── Cell templates (hook-free; read enriched row fields) ──────────────────
function GroupNameCell(row: GroupRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      {row.description && (
        <div className="truncate text-[12px] text-[var(--color-muted-foreground)]" title={row.description}>
          {row.description}
        </div>
      )}
    </div>
  );
}
function GroupCompositionCell(row: GroupRow) {
  return (
    <div className="flex flex-col text-[12px] text-[var(--color-muted-foreground)]">
      <span>{row.membersLabel}</span>
      <span>{row.rolesLabel}</span>
    </div>
  );
}
function GroupFlagsCell(row: GroupRow) {
  if (!row.isDefault && !row.isSystemGroup) {
    return <span className="text-[12px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">—</span>;
  }
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {row.isDefault && (
        <EntityStatusBadge tone="info">
          <Star className="mr-0.5 size-2.5" />
          {row.defaultLabel}
        </EntityStatusBadge>
      )}
      {row.isSystemGroup && <EntityStatusBadge tone="default">{row.systemLabel}</EntityStatusBadge>}
    </div>
  );
}

export function GroupsPage() {
  const { t } = useTranslation("identity");
  const { t: tc } = useTranslation("common");
  const navigate = useNavigate();
  const [createOpen, setCreateOpen] = useState(false);
  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");

  const query = useQuery({
    queryKey: ["identity", "groups", "list"],
    queryFn: () => listGroups(),
  });

  const allGroups = useMemo(() => query.data ?? [], [query.data]);

  const rows: GroupRow[] = useMemo(() => {
    const q = search.trim().toLowerCase();
    return allGroups
      .filter(
        (g) =>
          !q ||
          g.name.toLowerCase().includes(q) ||
          (g.description ?? "").toLowerCase().includes(q),
      )
      .map((g) => ({
        id: g.id,
        name: g.name,
        description: g.description ?? "",
        membersLabel: t("groups.membersCount", { count: g.memberCount }),
        rolesLabel: t("groups.rolesCount", { count: g.roleNames?.length ?? 0 }),
        isDefault: g.isDefault,
        isSystemGroup: g.isSystemGroup,
        defaultLabel: t("groups.defaultRoleLabel"),
        systemLabel: t("groups.systemBadge"),
      }));
  }, [allGroups, search, t]);

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "name", headerText: t("groups.columns.group"), template: GroupNameCell as any, minWidth: 240 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "membersLabel", headerText: t("groups.columns.composition"), template: GroupCompositionCell as any, width: 190, allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isDefault", headerText: t("groups.columns.flags"), template: GroupFlagsCell as any, width: 190, allowSorting: false },
    ],
    [t],
  );

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UsersRound}
        title={t("groups.title")}
        total={query.data ? allGroups.length : null}
        unit={t("groups.singular")}
        description={t("groups.description")}
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
          perm={P.identity.groups.create}
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("groups.actions.create")}
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
              placeholder={t("groups.searchPlaceholder")}
              ariaLabel={tc("gridFilters.search")}
              className="min-w-64"
            />
          </MakaFilterField>
        }
      />

      <MakaGridClient<GroupRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="grupos"
        entityName={t("groups.singular")}
        onRowClick={(row) => navigate(`/identity/groups/${row.id}`)}
        onClearFilters={() => setSearch("")}
      />

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <span>{describe(query.error)}</span>
        </div>
      )}

      <CreateGroupDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}


function CreateGroupDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation("identity");
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isDefault, setIsDefault] = useState(false);

  useEffect(() => {
    if (!open) {
      setName("");
      setDescription("");
      setIsDefault(false);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: () =>
      createGroup({
        name: name.trim(),
        description: description.trim() || undefined,
        isDefault,
        roleIds: [],
      }),
    onSuccess: (group) => {
      toast.success(t("groups.groupCreated"), { description: t("groups.groupCreatedDesc") });
      void queryClient.invalidateQueries({ queryKey: ["identity", "groups"] });
      onClose();
      navigate(`/identity/groups/${group.id}`);
    },
    onError: (err) => toast.error(t("groups.createFailed"), { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim()) return;
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("groups.createTitle")}</DialogTitle>
            <DialogDescription>{t("groups.createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="group-name" span={4} label={t("groups.fields.name")} required>
                <Input
                  id="group-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t("groups.groupNamePlaceholder")}
                  required
                  autoFocus
                  maxLength={128}
                />
              </Field>
              <Field
                id="group-description"
                span={8}
                label={t("groups.fields.description")}
                hint={t("groups.descriptionHint")}
              >
                <Input
                  id="group-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder={t("groups.groupDescPlaceholder")}
                  maxLength={512}
                />
              </Field>
              <div className="col-span-1 flex items-center justify-between gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3 sm:col-span-12">
              <div className="min-w-0">
                <span className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("groups.defaultGroupLabel")}
                </span>
                <span className="mt-0.5 block text-[12.5px] text-[var(--color-muted-foreground)]">
                  {t("groups.isDefaultDesc")}
                </span>
              </div>
              <Switch
                checked={isDefault}
                onCheckedChange={setIsDefault}
                aria-label={t("groups.defaultGroupLabel")}
              />
              </div>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !name.trim()}>
              {mutation.isPending ? t("groups.creating") : t("groups.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
