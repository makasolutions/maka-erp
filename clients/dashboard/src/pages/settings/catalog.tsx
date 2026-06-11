import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Factory, Info, Languages, Pencil, Plus, Receipt, Trash2, Truck, X } from "lucide-react";
import { toast } from "sonner";
import {
  createShippingClass, createTaxRate, deleteShippingClass, deleteTaxRate,
  getShippingClasses, getTaxRates, updateShippingClass, updateTaxRate,
  type ShippingClassDto, type TaxRateDto,
} from "@/api/catalog";
import {
  addCatalogAlias, deleteCatalogAlias, getCatalogAliases, getIndustries, getTenantIndustries,
  searchGlobalBrands, searchGlobalCategories, setTenantIndustries,
  type CatalogAliasEntity, type GlobalBrandSuggestion, type GlobalCategorySuggestion,
} from "@/api/catalog-global";
import { GlobalSuggestionField } from "@/components/catalog/global-suggestion-field";
import { SettingsSection } from "@/pages/settings/settings-layout";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, EntityStatusBadge, Field, FormGrid } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";

const PERM = { view: "Permissions.Catalog.Settings.View", manage: "Permissions.Catalog.Settings.Manage" };

export function CatalogSettings() {
  const { t } = useTranslation("settings");
  return (
    <div className="space-y-5">
      <IndustriesSection />
      <AliasesSection />
      <TaxRatesSection />
      <ShippingClassesSection />
      <SettingsSection title={t("catalogSettings.statuses")} icon={Info}>
        <div className="px-5 py-4 text-[13px] text-[var(--color-muted-foreground)]">{t("catalogSettings.statusesHint")}</div>
      </SettingsSection>
    </div>
  );
}

// ── Tax rates ──────────────────────────────────────────────────────────────

type TaxEditor = { mode: "closed" } | { mode: "create" } | { mode: "edit"; item: TaxRateDto };

function TaxRatesSection() {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const queryClient = useQueryClient();
  const [editor, setEditor] = useState<TaxEditor>({ mode: "closed" });

  const q = useQuery({ queryKey: ["catalog", "tax-rates"], queryFn: () => getTaxRates(), placeholderData: keepPreviousData });
  const del = useMutation({
    mutationFn: (id: string) => deleteTaxRate(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: ["catalog", "tax-rates"] }); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });
  const canManage = can(PERM.manage);

  return (
    <SettingsSection
      title={t("catalogSettings.taxRates")} icon={Receipt} description={t("catalogSettings.taxRatesHint")}
      footer={canManage && <Button size="sm" onClick={() => setEditor({ mode: "create" })}><Plus className="size-4" />{t("catalogSettings.add")}</Button>}
    >
      <ul className="divide-y divide-[var(--color-border)]">
        {(q.data ?? []).map((it) => (
          <li key={it.id} className="flex items-center justify-between gap-3 px-5 py-3">
            <div className="flex items-center gap-2 text-[13px]">
              <span className="font-medium text-[var(--color-foreground)]">{it.name}</span>
              <span className="font-mono text-[var(--color-muted-foreground)]">{Math.round(it.rate * 100)}%</span>
              {it.isDefault && <EntityStatusBadge tone="info">{t("catalogSettings.default")}</EntityStatusBadge>}
              {!it.isActive && <EntityStatusBadge tone="default">{tc("status.inactive")}</EntityStatusBadge>}
            </div>
            {canManage && (
              <div className="flex items-center gap-1">
                <Button size="icon" variant="ghost" onClick={() => setEditor({ mode: "edit", item: it })} aria-label={t("catalogSettings.edit")}><Pencil className="size-4" /></Button>
                <Button size="icon" variant="ghost" onClick={() => del.mutate(it.id)} aria-label={t("catalogSettings.delete")} className="hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></Button>
              </div>
            )}
          </li>
        ))}
        {(q.data ?? []).length === 0 && <li className="px-5 py-4 text-[13px] text-[var(--color-muted-foreground)]">{t("catalogSettings.empty")}</li>}
      </ul>
      <TaxRateDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </SettingsSection>
  );
}

function TaxRateDialog({ state, onClose }: { state: TaxEditor; onClose: () => void }) {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "create" || state.mode === "edit";
  const item = state.mode === "edit" ? state.item : undefined;

  const [name, setName] = useState("");
  const [ratePct, setRatePct] = useState("");
  const [description, setDescription] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!isOpen) return;
    setName(item?.name ?? "");
    setRatePct(item ? String(Math.round(item.rate * 100)) : "");
    setDescription(item?.description ?? "");
    setIsDefault(item?.isDefault ?? false);
    setIsActive(item?.isActive ?? true);
  }, [isOpen, item]);

  const onDone = () => { queryClient.invalidateQueries({ queryKey: ["catalog", "tax-rates"] }); onClose(); };
  const create = useMutation({ mutationFn: () => createTaxRate({ name: name.trim(), rate: Number(ratePct) / 100, description: description.trim() || null, isDefault }), onSuccess: () => { toast.success(tc("feedback.created")); onDone(); }, onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }) });
  const update = useMutation({ mutationFn: () => updateTaxRate({ id: item!.id, name: name.trim(), rate: Number(ratePct) / 100, description: description.trim() || null, isDefault, isActive }), onSuccess: () => { toast.success(tc("feedback.updated")); onDone(); }, onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }) });

  const rateNum = Number(ratePct);
  const canSubmit = !!name.trim() && ratePct !== "" && rateNum >= 0 && rateNum <= 100;
  const submit = (e: FormEvent) => { e.preventDefault(); if (!canSubmit) return; (item ? update : create).mutate(); };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={submit}>
          <DialogHeader><DialogTitle>{item ? t("catalogSettings.edit") : t("catalogSettings.add")} · {t("catalogSettings.taxRates")}</DialogTitle><DialogDescription>{t("catalogSettings.taxRatesHint")}</DialogDescription></DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="tx-name" span={8} label={t("catalogSettings.name")} required>
                <Input id="tx-name" value={name} onChange={(e) => setName(e.target.value)} maxLength={64} required autoFocus placeholder={t("catalogSettings.namePlaceholder")} />
              </Field>
              <Field id="tx-rate" span={4} label={t("catalogSettings.rate")} required>
                <Input id="tx-rate" type="number" min={0} max={100} step="0.01" value={ratePct} onChange={(e) => setRatePct(e.target.value)} required placeholder={t("catalogSettings.ratePlaceholder")} />
              </Field>
              <Field id="tx-desc" span={12} label={t("catalogSettings.description")}>
                <Input id="tx-desc" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={256} />
              </Field>
              <div className="col-span-1 flex items-center gap-8 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]"><Switch checked={isDefault} onCheckedChange={setIsDefault} />{t("catalogSettings.default")}</label>
                {item && <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]"><Switch checked={isActive} onCheckedChange={setIsActive} />{t("catalogSettings.active")}</label>}
              </div>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline"><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
            <Button type="submit" disabled={!canSubmit || create.isPending || update.isPending}><Check className="size-4" />{create.isPending || update.isPending ? tc("feedback.saving") : t("catalogSettings.save")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Shipping classes ─────────────────────────────────────────────────────────

type ShipEditor = { mode: "closed" } | { mode: "create" } | { mode: "edit"; item: ShippingClassDto };

function ShippingClassesSection() {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const queryClient = useQueryClient();
  const [editor, setEditor] = useState<ShipEditor>({ mode: "closed" });

  const q = useQuery({ queryKey: ["catalog", "shipping-classes"], queryFn: () => getShippingClasses(), placeholderData: keepPreviousData });
  const del = useMutation({
    mutationFn: (id: string) => deleteShippingClass(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: ["catalog", "shipping-classes"] }); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });
  const canManage = can(PERM.manage);

  return (
    <SettingsSection
      title={t("catalogSettings.shippingClasses")} icon={Truck} description={t("catalogSettings.shippingClassesHint")}
      footer={canManage && <Button size="sm" onClick={() => setEditor({ mode: "create" })}><Plus className="size-4" />{t("catalogSettings.add")}</Button>}
    >
      <ul className="divide-y divide-[var(--color-border)]">
        {(q.data ?? []).map((it) => (
          <li key={it.id} className="flex items-center justify-between gap-3 px-5 py-3">
            <div className="min-w-0 text-[13px]">
              <span className="font-medium text-[var(--color-foreground)]">{it.name}</span>
              {it.description && <span className="ml-2 text-[var(--color-muted-foreground)]">{it.description}</span>}
            </div>
            {canManage && (
              <div className="flex items-center gap-1">
                <Button size="icon" variant="ghost" onClick={() => setEditor({ mode: "edit", item: it })} aria-label={t("catalogSettings.edit")}><Pencil className="size-4" /></Button>
                <Button size="icon" variant="ghost" onClick={() => del.mutate(it.id)} aria-label={t("catalogSettings.delete")} className="hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></Button>
              </div>
            )}
          </li>
        ))}
        {(q.data ?? []).length === 0 && <li className="px-5 py-4 text-[13px] text-[var(--color-muted-foreground)]">{t("catalogSettings.empty")}</li>}
      </ul>
      <ShippingClassDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </SettingsSection>
  );
}

function ShippingClassDialog({ state, onClose }: { state: ShipEditor; onClose: () => void }) {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "create" || state.mode === "edit";
  const item = state.mode === "edit" ? state.item : undefined;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  useEffect(() => { if (!isOpen) return; setName(item?.name ?? ""); setDescription(item?.description ?? ""); }, [isOpen, item]);

  const onDone = () => { queryClient.invalidateQueries({ queryKey: ["catalog", "shipping-classes"] }); onClose(); };
  const create = useMutation({ mutationFn: () => createShippingClass({ name: name.trim(), description: description.trim() || null }), onSuccess: () => { toast.success(tc("feedback.created")); onDone(); }, onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }) });
  const update = useMutation({ mutationFn: () => updateShippingClass({ id: item!.id, name: name.trim(), description: description.trim() || null }), onSuccess: () => { toast.success(tc("feedback.updated")); onDone(); }, onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }) });

  const canSubmit = !!name.trim();
  const submit = (e: FormEvent) => { e.preventDefault(); if (!canSubmit) return; (item ? update : create).mutate(); };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={submit}>
          <DialogHeader><DialogTitle>{item ? t("catalogSettings.edit") : t("catalogSettings.add")} · {t("catalogSettings.shippingClasses")}</DialogTitle><DialogDescription>{t("catalogSettings.shippingClassesHint")}</DialogDescription></DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="sc-name" span={12} label={t("catalogSettings.name")} required>
                <Input id="sc-name" value={name} onChange={(e) => setName(e.target.value)} maxLength={64} required autoFocus />
              </Field>
              <Field id="sc-desc" span={12} label={t("catalogSettings.description")}>
                <Input id="sc-desc" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={256} />
              </Field>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="outline"><X className="size-4" />{tc("actions.cancel")}</Button></DialogClose>
            <Button type="submit" disabled={!canSubmit || create.isPending || update.isPending}><Check className="size-4" />{create.isPending || update.isPending ? tc("feedback.saving") : t("catalogSettings.save")}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Industrias del tenant ────────────────────────────────────────────────────

function IndustriesSection() {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const queryClient = useQueryClient();
  const canManage = can("Permissions.Catalog.Categories.Update");

  const industriesQ = useQuery({ queryKey: ["catalog", "industries"], queryFn: getIndustries });
  const selectedQ = useQuery({ queryKey: ["catalog", "tenant-industries"], queryFn: getTenantIndustries });

  const [selected, setSelected] = useState<Set<string>>(new Set());
  useEffect(() => {
    if (selectedQ.data) setSelected(new Set(selectedQ.data));
  }, [selectedQ.data]);

  const save = useMutation({
    mutationFn: () => setTenantIndustries([...selected]),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "tenant-industries"] });
    },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const toggle = (id: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });

  return (
    <SettingsSection
      title={t("catalogSettings.industries.title")}
      icon={Factory}
      description={t("catalogSettings.industries.hint")}
      footer={canManage && (
        <Button size="sm" onClick={() => save.mutate()} disabled={save.isPending}>
          <Check className="size-4" />{save.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}
        </Button>
      )}
    >
      <div className="grid gap-2 px-5 py-4 sm:grid-cols-2 lg:grid-cols-3">
        {(industriesQ.data ?? []).map((ind) => (
          <label key={ind.id} className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
            <Switch checked={selected.has(ind.id)} disabled={!canManage} onCheckedChange={() => toggle(ind.id)} />
            {ind.name}
          </label>
        ))}
        {(industriesQ.data ?? []).length === 0 && (
          <p className="text-[13px] text-[var(--color-muted-foreground)]">{t("catalogSettings.industries.empty")}</p>
        )}
      </div>
    </SettingsSection>
  );
}

// ── Sinónimos / alias de búsqueda ────────────────────────────────────────────

function AliasesSection() {
  const { t } = useTranslation("settings");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();
  const queryClient = useQueryClient();
  const canManage = can("Permissions.Catalog.Categories.Update");

  const [entity, setEntity] = useState<CatalogAliasEntity>("Brand");
  const [targetText, setTargetText] = useState("");
  const [target, setTarget] = useState<{ id: string; name: string } | null>(null);
  const [alias, setAlias] = useState("");

  const listQ = useQuery({ queryKey: ["catalog", "aliases"], queryFn: () => getCatalogAliases() });

  const reset = () => { setTargetText(""); setTarget(null); setAlias(""); };
  const add = useMutation({
    mutationFn: () => addCatalogAlias(entity, target!.id, alias.trim()),
    onSuccess: () => { toast.success(t("catalogSettings.aliases.added")); queryClient.invalidateQueries({ queryKey: ["catalog", "aliases"] }); reset(); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });
  const del = useMutation({
    mutationFn: (id: string) => deleteCatalogAlias(id),
    onSuccess: () => { toast.success(t("catalogSettings.aliases.deleted")); queryClient.invalidateQueries({ queryKey: ["catalog", "aliases"] }); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  const canAdd = !!target && alias.trim().length >= 2;

  return (
    <SettingsSection title={t("catalogSettings.aliases.title")} icon={Languages} description={t("catalogSettings.aliases.hint")}>
      {canManage && (
        <div className="grid gap-2 px-5 py-4 sm:grid-cols-12">
          <div className="sm:col-span-3">
            <Combobox id="alias-entity" label={t("catalogSettings.aliases.entity")} value={entity}
              onChange={(v) => { if (v) { setEntity(v as CatalogAliasEntity); setTarget(null); setTargetText(""); } }}
              options={[{ value: "Brand", label: t("catalogSettings.aliases.brand") }, { value: "Category", label: t("catalogSettings.aliases.category") }]} />
          </div>
          <div className="sm:col-span-5">
            {entity === "Brand" ? (
              <GlobalSuggestionField<GlobalBrandSuggestion>
                id="alias-target-b" value={targetText} onChange={(v) => { setTargetText(v); setTarget(null); }}
                search={searchGlobalBrands} queryKey="alias-brands"
                toItem={(s) => ({ key: s.id, primary: s.name, secondary: s.country, adopted: false })}
                onPick={(s) => { setTarget({ id: s.id, name: s.name }); setTargetText(s.name); }}
                placeholder={t("catalogSettings.aliases.targetPlaceholder")} />
            ) : (
              <GlobalSuggestionField<GlobalCategorySuggestion>
                id="alias-target-c" value={targetText} onChange={(v) => { setTargetText(v); setTarget(null); }}
                search={searchGlobalCategories} queryKey="alias-cats"
                toItem={(s) => ({ key: s.id, primary: s.name, secondary: s.fullPath, adopted: false })}
                onPick={(s) => { setTarget({ id: s.id, name: s.name }); setTargetText(s.name); }}
                placeholder={t("catalogSettings.aliases.targetPlaceholder")} />
            )}
          </div>
          <div className="sm:col-span-3">
            <Input value={alias} onChange={(e) => setAlias(e.target.value)} maxLength={128}
              placeholder={t("catalogSettings.aliases.aliasPlaceholder")} aria-label={t("catalogSettings.aliases.alias")} />
          </div>
          <div className="sm:col-span-1 flex items-center">
            <Button size="sm" disabled={!canAdd || add.isPending} onClick={() => add.mutate()}><Plus className="size-4" /></Button>
          </div>
        </div>
      )}
      <ul className="divide-y divide-[var(--color-border)] border-t border-[var(--color-border)]">
        {(listQ.data ?? []).map((a) => (
          <li key={a.id} className="flex items-center justify-between gap-3 px-5 py-2.5 text-[13px]">
            <div className="flex min-w-0 items-center gap-2">
              <EntityStatusBadge tone="info">{a.entityType === "Brand" ? t("catalogSettings.aliases.brand") : t("catalogSettings.aliases.category")}</EntityStatusBadge>
              <span className="font-mono text-[var(--color-foreground)]">{a.alias}</span>
              <span className="text-[var(--color-muted-foreground)]">→ {a.targetName ?? "—"}</span>
            </div>
            {canManage && (
              <Button size="icon" variant="ghost" onClick={() => del.mutate(a.id)} aria-label={t("catalogSettings.delete")} className="hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></Button>
            )}
          </li>
        ))}
        {(listQ.data ?? []).length === 0 && <li className="px-5 py-4 text-[13px] text-[var(--color-muted-foreground)]">{t("catalogSettings.aliases.empty")}</li>}
      </ul>
    </SettingsSection>
  );
}
