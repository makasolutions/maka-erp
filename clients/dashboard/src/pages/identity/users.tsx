import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, Plus, UserPlus, Users, X } from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { toast } from "sonner";
import {
  listRoles,
  registerUser,
  searchUsers,
  type UserDto,
  type RegisterUserInput,
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
import {
  Combobox,
  EntityFilterPill,
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

type StatusFilter = "all" | "active" | "inactive";
type EmailFilter = "all" | "confirmed" | "unconfirmed";

type UserRow = UserDto & { activeLabel: string; confirmLabel: string };

// Filter dropdown surface — matches the rest of the filter row.
const USER_FILTER_COMBO =
  "h-8 w-52 rounded-md border-[var(--color-border)] bg-[var(--color-card)] shadow-none " +
  "hover:border-[var(--color-border)] hover:bg-[var(--color-accent)]";

// ── Cell templates (hook-free; read enriched row fields) ──────────────────
function UserNameCell(row: UserRow) {
  return <code className="font-mono text-[13px] text-[var(--color-foreground)]">{row.userName ? `@${row.userName}` : "—"}</code>;
}
function UserActiveCell(row: UserRow) {
  return <EntityStatusBadge tone={row.isActive ? "success" : "default"}>{row.activeLabel}</EntityStatusBadge>;
}
function UserConfirmCell(row: UserRow) {
  return <EntityStatusBadge tone={row.emailConfirmed ? "info" : "warning"}>{row.confirmLabel}</EntityStatusBadge>;
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function UsersPage() {
  const { t } = useTranslation("identity");
  const { t: tc } = useTranslation("common");
  const navigate = useNavigate();
  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [emailFilter, setEmailFilter] = useState<EmailFilter>("all");
  const [roleFilter, setRoleFilter] = useState<string | null>(null);
  const [registerOpen, setRegisterOpen] = useState(false);

  // Client-side grid: fetch the full set once; search / status / email filter
  // locally. The role filter stays server-side (UserDto carries no roles), so
  // it drives the fetch.
  const query = useQuery({
    queryKey: ["identity", "users", "list", roleFilter],
    queryFn: () =>
      searchUsers({ pageNumber: 1, pageSize: 100, sort: "userName asc", roleId: roleFilter }),
    placeholderData: keepPreviousData,
  });

  const rolesQuery = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
    staleTime: 60_000,
  });

  const allItems = query.data?.items ?? [];

  const rows: UserRow[] = useMemo(() => {
    const q = search.trim().toLowerCase();
    return allItems
      .filter(
        (u) =>
          (statusFilter === "all" || u.isActive === (statusFilter === "active")) &&
          (emailFilter === "all" || u.emailConfirmed === (emailFilter === "confirmed")) &&
          (!q ||
            [u.firstName, u.lastName, u.email, u.userName, u.phoneNumber]
              .filter(Boolean)
              .some((v) => (v as string).toLowerCase().includes(q))),
      )
      .map((u) => ({
        ...u,
        activeLabel: u.isActive ? t("users.filters.active") : t("users.filters.inactive"),
        confirmLabel: u.emailConfirmed ? t("users.filters.confirmed") : t("users.filters.unconfirmed"),
      }));
  }, [allItems, search, statusFilter, emailFilter, t]);

  const columns: ColumnModel[] = useMemo(
    () => [
      { field: "firstName", headerText: t("users.fields.firstName"), minWidth: 120 },
      { field: "lastName", headerText: t("users.fields.lastName"), minWidth: 120 },
      { field: "email", headerText: t("users.fields.email"), minWidth: 200 },
      { field: "phoneNumber", headerText: t("users.fields.phone"), width: 150 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "userName", headerText: t("users.fields.username"), template: UserNameCell as any, width: 170 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isActive", headerText: t("users.columns.active"), template: UserActiveCell as any, width: 120, allowSorting: false, textAlign: "Center" },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "emailConfirmed", headerText: t("users.columns.confirmation"), template: UserConfirmCell as any, width: 140, allowSorting: false, textAlign: "Center" },
    ],
    [t],
  );

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setEmailFilter("all");
    setRoleFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Users}
        title={t("users.title")}
        total={query.data?.totalCount ?? null}
        unit={t("users.singular")}
        description={t("users.description")}
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
          perm={P.identity.users.create}
          onClick={() => setRegisterOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("users.register")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={clearFilters}
        filters={
          <>
            <MakaFilterField label={t("users.search")} className="grow">
              <MakaFilterInput
                value={search}
                onChange={setSearch}
                placeholder={t("users.searchPlaceholder")}
                ariaLabel={t("users.search")}
                className="min-w-64"
              />
            </MakaFilterField>
            <MakaFilterField label={t("users.accountStatus")}>
              <EntityFilterPill
                label={t("users.accountStatus")}
                value={statusFilter}
                onChange={setStatusFilter}
                options={[
                  { value: "all", label: t("users.filters.allStatus") },
                  { value: "active", label: t("users.filters.active") },
                  { value: "inactive", label: t("users.filters.inactive") },
                ]}
              />
            </MakaFilterField>
            <MakaFilterField label={t("users.emailStatus")}>
              <EntityFilterPill
                label={t("users.emailStatus")}
                value={emailFilter}
                onChange={setEmailFilter}
                options={[
                  { value: "all", label: t("users.filters.allEmail") },
                  { value: "confirmed", label: t("users.filters.confirmed") },
                  { value: "unconfirmed", label: t("users.filters.unconfirmed") },
                ]}
              />
            </MakaFilterField>
            <MakaFilterField label={t("users.fields.role")}>
              <Combobox
                id="users-role-filter"
                label={t("users.fields.role")}
                placeholder={t("users.filters.allStatus")}
                value={roleFilter}
                onChange={setRoleFilter}
                options={(rolesQuery.data ?? []).map((r) => ({ value: r.id, label: r.name }))}
                searchable
                clearable
                emptyOptionLabel={t("users.filters.allStatus")}
                className={USER_FILTER_COMBO}
              />
            </MakaFilterField>
          </>
        }
      />

      <MakaGridClient<UserRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="usuarios"
        entityName={t("users.singular")}
        onRowClick={(row) => row.id && navigate(`/identity/users/${row.id}`)}
        onClearFilters={clearFilters}
      />

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <RegisterUserDialog
        open={registerOpen}
        onClose={() => setRegisterOpen(false)}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Register dialog — hooks/mutations preserved verbatim
// ───────────────────────────────────────────────────────────────────────

function RegisterUserDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation("identity");
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");

  useEffect(() => {
    if (!open) {
      setFirstName("");
      setLastName("");
      setEmail("");
      setUserName("");
      setPassword("");
      setConfirmPassword("");
      setPhoneNumber("");
    }
  }, [open]);

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;

  const mutation = useMutation({
    mutationFn: (input: RegisterUserInput) => registerUser(input),
    onSuccess: () => {
      toast.success(t("users.registered"), {
        description: t("users.registeredDesc"),
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      onClose();
    },
    onError: (err) =>
      toast.error(t("users.registrationFailed"), { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (passwordMismatch) return;
    mutation.mutate({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim(),
      userName: userName.trim(),
      password,
      confirmPassword,
      phoneNumber: phoneNumber.trim() || undefined,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("users.registerMember")}</DialogTitle>
            <DialogDescription>{t("users.registerDesc")}</DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="reg-first" span={6} label={t("users.fields.firstName")} required>
                <Input
                  id="reg-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder={t("users.fields.firstNamePlaceholder")}
                  autoFocus
                  required
                />
              </Field>
              <Field id="reg-last" span={6} label={t("users.fields.lastName")} required>
                <Input
                  id="reg-last"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder={t("users.fields.lastNamePlaceholder")}
                  required
                />
              </Field>

              <Field
                id="reg-username"
                span={6}
                label={t("users.fields.username")}
                required
                hint={t("users.fields.usernameHint")}
              >
                <Input
                  id="reg-username"
                  value={userName}
                  onChange={(e) => setUserName(e.target.value)}
                  placeholder="ada.lovelace"
                  autoComplete="off"
                  required
                />
              </Field>

              <Field id="reg-email" span={6} label={t("users.fields.email")} required>
                <Input
                  id="reg-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="ada@example.com"
                  required
                />
              </Field>

              <Field id="reg-phone" span={12} label={t("users.fields.phone")} hint={t("users.fields.phoneHint")}>
                <Input
                  id="reg-phone"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  placeholder="+44 …"
                />
              </Field>

              <Field id="reg-pwd" span={6} label={t("users.fields.password")} required>
                <Input
                  id="reg-pwd"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                />
              </Field>
              <Field
                id="reg-pwd2"
                span={6}
                label={t("users.fields.confirmPassword")}
                required
                hint={passwordMismatch ? t("users.fields.passwordMismatch") : undefined}
              >
                <Input
                  id="reg-pwd2"
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                  aria-invalid={passwordMismatch || undefined}
                />
              </Field>
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button
                type="button"
                variant="outline"
                disabled={mutation.isPending}
              >
                <X className="size-4" />{t("common:actions.cancel")}
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || passwordMismatch}
              className="gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? t("users.registering") : t("users.register")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

