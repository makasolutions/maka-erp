import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ListChecks, Plus, Trash2, X } from "lucide-react";
import { SaveIcon, CreateIcon, CancelIcon } from "@/components/ui/icons";
import { toast } from "sonner";
import {
  createCustomFieldDefinition,
  deleteCustomFieldDefinition,
  getCustomFieldDefinitions,
  updateCustomFieldDefinition,
  type CustomFieldDefinitionDto,
  type CustomFieldEntityType,
  type CustomFieldOption,
  type CustomFieldType,
} from "@/api/custom-fields";
import { SettingsSection } from "@/pages/settings/settings-layout";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, EntityStatusBadge, Field, FormActions, FormGrid } from "@/components/list";
import { MakaGridClient } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe, slugify } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const FIELD_TYPES: CustomFieldType[] = [
  "Text", "Number", "Currency", "Date", "Checkbox", "Select", "MultiSelect", "EmailAddress", "PhoneNumber",
];
const ENTITY_TYPES: CustomFieldEntityType[] = ["Party", "PartyRelationship"];
const isChoice = (t: CustomFieldType) => t === "Select" || t === "MultiSelect";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; def: CustomFieldDefinitionDto }
  | { mode: "delete"; def: CustomFieldDefinitionDto };

type DefRow = CustomFieldDefinitionDto & { scopeLabel: string; typeLabel: string };

export function CustomFieldsSettings() {
  const { t } = useTranslation("parties");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [scope, setScope] = useState<CustomFieldEntityType | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: ["parties", "custom-fields", "list"],
    queryFn: () => getCustomFieldDefinitions({ includeInactive: false }),
    placeholderData: keepPreviousData,
  });

  const all = query.data ?? [];

  const rows: DefRow[] = useMemo(
    () =>
      all
        .filter((d) => !scope || d.entityType === scope)
        .map((d) => ({
          ...d,
          scopeLabel: t(`customFields.scope.${d.entityType}`),
          typeLabel: t(`customFields.types.${d.fieldType}`),
        })),
    [all, scope, t],
  );

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "title", headerText: t("customFields.fields.title"), template: TitleCell as any, minWidth: 220 },
      { field: "scopeLabel", headerText: t("customFields.fields.entityType"), width: 130 },
      { field: "typeLabel", headerText: t("customFields.fields.type"), width: 140 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "isRequired", headerText: t("customFields.fields.required"), template: RequiredCell as any, width: 120, textAlign: "Center", allowSorting: false },
    ],
    [t],
  );

  return (
    <SettingsSection
      title={t("customFields.title")}
      icon={ListChecks}
      description={t("customFields.description")}
    >
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="max-w-xs grow">
            <Combobox
              label={t("customFields.filterScope")}
              variant="filter"
              value={scope}
              onChange={(v) => setScope(v as CustomFieldEntityType | null)}
              clearable
              emptyOptionLabel={tc("status.all")}
              options={ENTITY_TYPES.map((e) => ({ value: e, label: t(`customFields.scope.${e}`) }))}
            />
          </div>
          <Button
            perm={P.parties.customFields.define}
            onClick={() => setEditor({ mode: "create" })}
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
          >
            <Plus className="size-4" />
            {t("customFields.actions.create")}
          </Button>
        </div>

        <MakaGridClient<DefRow>
          dataSource={rows}
          columns={columns}
          isLoading={query.isFetching}
          fileName="campos-personalizados"
          entityName={t("customFields.singular")}
          onRowClick={(row) => can(P.parties.customFields.define) && setEditor({ mode: "edit", def: row })}
          permissions={{ edit: P.parties.customFields.define, delete: P.parties.customFields.define }}
          onEdit={(row) => setEditor({ mode: "edit", def: row })}
          onDelete={(row) => setEditor({ mode: "delete", def: row })}
        />
      </div>

      <CustomFieldEditorDialog state={editor} existing={all} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteCustomFieldDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </SettingsSection>
  );
}

function TitleCell(row: DefRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.title}</div>
      <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">{row.apiSlug}</code>
    </div>
  );
}

function RequiredCell(row: DefRow) {
  if (!row.isRequired) return null;
  return <EntityStatusBadge tone="info">•</EntityStatusBadge>;
}

// ───────────────────────────────────────────────────────────────────────
//  Editor dialog
// ───────────────────────────────────────────────────────────────────────

function CustomFieldEditorDialog({
  state,
  existing,
  onClose,
}: {
  state: EditorState;
  existing: CustomFieldDefinitionDto[];
  onClose: () => void;
}) {
  const { t } = useTranslation("parties");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const isOpen = state.mode === "create" || state.mode === "edit";
  const def = state.mode === "edit" ? state.def : undefined;

  const [entityType, setEntityType] = useState<CustomFieldEntityType>("Party");
  const [title, setTitle] = useState("");
  const [slug, setSlug] = useState("");
  const [slugTouched, setSlugTouched] = useState(false);
  const [fieldType, setFieldType] = useState<CustomFieldType>("Text");
  const [description, setDescription] = useState("");
  const [isRequired, setIsRequired] = useState(false);
  const [isUnique, setIsUnique] = useState(false);
  const [options, setOptions] = useState<CustomFieldOption[]>([]);
  const [showErrors, setShowErrors] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setEntityType(def?.entityType ?? "Party");
    setTitle(def?.title ?? "");
    setSlug(def?.apiSlug ?? "");
    setSlugTouched(Boolean(def));
    setFieldType(def?.fieldType ?? "Text");
    setDescription(def?.description ?? "");
    setIsRequired(def?.isRequired ?? false);
    setIsUnique(def?.isUnique ?? false);
    setOptions(def?.options ?? []);
    setShowErrors(false);
  }, [isOpen, def]);

  const effectiveSlug = useMemo(
    () => slugify(slugTouched && slug ? slug : title),
    [slug, slugTouched, title],
  );

  const slugTaken = useMemo(() => {
    if (!effectiveSlug) return false;
    return existing.some(
      (d) => d.activo && d.entityType === entityType && d.apiSlug === effectiveSlug && d.id !== def?.id,
    );
  }, [existing, effectiveSlug, entityType, def?.id]);

  const errs = useMemo(() => {
    if (!showErrors) return {} as Record<string, string>;
    const e: Record<string, string> = {};
    if (!title.trim()) e.title = tc("validation.required");
    if (!effectiveSlug) e.slug = t("customFields.slugInvalid");
    else if (slugTaken) e.slug = t("customFields.slugTaken");
    if (isChoice(fieldType) && options.filter((o) => o.value.trim()).length === 0)
      e.options = t("customFields.optionsRequired");
    return e;
  }, [showErrors, title, effectiveSlug, slugTaken, fieldType, options, t, tc]);

  const createMutation = useMutation({
    mutationFn: createCustomFieldDefinition,
    onSuccess: () => {
      toast.success(tc("feedback.created"));
      queryClient.invalidateQueries({ queryKey: ["parties", "custom-fields"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.createFailed"), { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateCustomFieldDefinition,
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["parties", "custom-fields"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.updateFailed"), { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const hasError = !title.trim() || !effectiveSlug || slugTaken ||
      (isChoice(fieldType) && options.filter((o) => o.value.trim()).length === 0);
    if (hasError) { setShowErrors(true); return; }

    const cleanOptions = isChoice(fieldType)
      ? options
          .filter((o) => o.value.trim())
          .map((o) => ({ value: o.value.trim(), label: o.label.trim() || o.value.trim(), color: o.color?.trim() || null }))
      : null;

    if (def) {
      updateMutation.mutate({
        id: def.id,
        title: title.trim(),
        description: description.trim() || null,
        isRequired,
        isUnique,
        isDefaultValueEnabled: false,
        defaultValue: null,
        isMultiselect: fieldType === "MultiSelect",
        options: cleanOptions,
      });
    } else {
      createMutation.mutate({
        entityType,
        title: title.trim(),
        apiSlug: slugTouched ? slug.trim() : null,
        fieldType,
        description: description.trim() || null,
        isRequired,
        isUnique,
        isDefaultValueEnabled: false,
        defaultValue: null,
        isMultiselect: fieldType === "MultiSelect",
        options: cleanOptions,
      });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{def ? t("customFields.actions.edit") : t("customFields.actions.add")}</DialogTitle>
            <DialogDescription>
              {def ? t("customFields.editDesc", { name: def.title }) : t("customFields.createDesc")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody>
            <FormGrid>
              <Field id="cf-entity" span={6} label={t("customFields.fields.entityType")}>
                <Combobox
                  id="cf-entity"
                  label={t("customFields.fields.entityType")}
                  variant="field"
                  value={entityType}
                  onChange={(v) => v && setEntityType(v as CustomFieldEntityType)}
                  disabled={Boolean(def)}
                  options={ENTITY_TYPES.map((e2) => ({ value: e2, label: t(`customFields.scope.${e2}`) }))}
                />
              </Field>

              <Field id="cf-type" span={6} label={t("customFields.fields.type")}>
                <Combobox
                  id="cf-type"
                  label={t("customFields.fields.type")}
                  variant="field"
                  value={fieldType}
                  onChange={(v) => v && setFieldType(v as CustomFieldType)}
                  disabled={Boolean(def)}
                  options={FIELD_TYPES.map((ft) => ({ value: ft, label: t(`customFields.types.${ft}`) }))}
                />
              </Field>

              <Field id="cf-title" span={6} label={t("customFields.fields.title")} required error={errs.title}>
                <Input
                  id="cf-title"
                  value={title}
                  onChange={(e2) => setTitle(e2.target.value)}
                  placeholder={t("customFields.placeholders.title")}
                  autoFocus
                  maxLength={128}
                />
              </Field>

              <Field id="cf-slug" span={6} label={t("customFields.fields.slug")} error={errs.slug} hint={t("customFields.hints.slug")}>
                <Input
                  id="cf-slug"
                  value={slugTouched ? slug : effectiveSlug}
                  onChange={(e2) => { setSlug(e2.target.value); setSlugTouched(true); }}
                  placeholder={t("customFields.placeholders.slug")}
                  disabled={Boolean(def)}
                  maxLength={64}
                  className="font-mono"
                />
              </Field>

              <Field id="cf-desc" span={12} label={t("customFields.fields.description")}>
                <Input
                  id="cf-desc"
                  value={description}
                  onChange={(e2) => setDescription(e2.target.value)}
                  maxLength={512}
                />
              </Field>

              {isChoice(fieldType) && (
                <div className="col-span-1 space-y-2 sm:col-span-12">
                  <div className="flex items-center justify-between">
                    <span className="text-[13px] font-medium text-[var(--color-foreground)]">{t("customFields.fields.options")}</span>
                    <Button type="button" variant="outline" size="sm" className="h-7 gap-1 text-[12px]"
                      onClick={() => setOptions((o) => [...o, { value: "", label: "", color: null }])}>
                      <Plus className="size-3.5" />{t("customFields.addOption")}
                    </Button>
                  </div>
                  {errs.options && <p className="text-[12px] text-[var(--color-destructive)]">{errs.options}</p>}
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">{t("customFields.hints.options")}</p>
                  <div className="space-y-2">
                    {options.map((opt, i) => (
                      <div key={i} className="flex items-center gap-2">
                        <Input
                          value={opt.value}
                          onChange={(e2) => setOptions((o) => o.map((x, j) => (j === i ? { ...x, value: e2.target.value } : x)))}
                          placeholder={t("customFields.placeholders.optionValue")}
                          className="font-mono"
                          aria-label={t("customFields.fields.optionValue")}
                        />
                        <Input
                          value={opt.label}
                          onChange={(e2) => setOptions((o) => o.map((x, j) => (j === i ? { ...x, label: e2.target.value } : x)))}
                          placeholder={t("customFields.placeholders.optionLabel")}
                          aria-label={t("customFields.fields.optionLabel")}
                        />
                        <input
                          type="color"
                          value={opt.color ?? "#888888"}
                          onChange={(e2) => setOptions((o) => o.map((x, j) => (j === i ? { ...x, color: e2.target.value } : x)))}
                          aria-label={t("customFields.fields.optionColor")}
                          className="h-9 w-10 shrink-0 cursor-pointer rounded-md border border-[var(--color-border)] bg-[var(--color-card)] p-0.5"
                        />
                        <Button type="button" variant="ghost" size="icon" className="size-9 shrink-0"
                          onClick={() => setOptions((o) => o.filter((_, j) => j !== i))} aria-label={tc("actions.delete")}>
                          <Trash2 className="size-4 text-[var(--color-destructive)]" />
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isRequired} onCheckedChange={setIsRequired} aria-label={t("customFields.fields.required")} />
                  {t("customFields.fields.required")}
                  <span className="text-[11.5px] font-normal text-[var(--color-muted-foreground)]">· {t("customFields.hints.required")}</span>
                </label>
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <Switch checked={isUnique} onCheckedChange={setIsUnique} aria-label={t("customFields.fields.unique")} />
                  {t("customFields.fields.unique")}
                  <span className="text-[11.5px] font-normal text-[var(--color-muted-foreground)]">· {t("customFields.hints.unique")}</span>
                </label>
              </div>
            </FormGrid>
          </DialogBody>

          <DialogFooter>
            <FormActions
              secondary={
                <DialogClose asChild>
                  <Button type="button" variant="outline" disabled={isPending}>
                    <CancelIcon className="size-4" />{tc("actions.cancel")}
                  </Button>
                </DialogClose>
              }
              primary={
                <Button type="submit" disabled={isPending}>
                  {def ? <SaveIcon className="size-4" /> : <CreateIcon className="size-4" />}
                  {isPending ? tc("feedback.saving") : def ? tc("actions.saveChanges") : t("customFields.actions.add")}
                </Button>
              }
            />
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Deactivate confirmation
// ───────────────────────────────────────────────────────────────────────

function DeleteCustomFieldDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("parties");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "delete";
  const def = state.mode === "delete" ? state.def : undefined;

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCustomFieldDefinition(id),
    onSuccess: () => {
      toast.success(tc("feedback.deleted"));
      queryClient.invalidateQueries({ queryKey: ["parties", "custom-fields"] });
      onClose();
    },
    onError: (err) => toast.error(tc("feedback.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{t("customFields.actions.delete")}</DialogTitle>
          <DialogDescription>{t("customFields.deleteDesc", { name: def?.title ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              <X className="size-4" />{tc("actions.cancel")}
            </Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => def && deleteMutation.mutate(def.id)} disabled={deleteMutation.isPending || !def}>
            <Trash2 className="size-4" />{deleteMutation.isPending ? tc("feedback.deleting") : t("customFields.actions.delete")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
