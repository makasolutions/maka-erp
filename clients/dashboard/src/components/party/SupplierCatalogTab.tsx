import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Tag, Building2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { Combobox } from "@/components/list";
import { getGlobalCategories } from "@/api/catalog-global";
import {
  getGlobalBrands, getSupplierBrands, getSupplierCategories, setSupplierBrands, setSupplierCategories,
} from "@/api/supplier-mapping";
import { setGlobalSupplier } from "@/api/parties";
import { describe } from "@/lib/list-helpers";

export function SupplierCatalogTab({
  partyId, isGlobalSupplier, onGlobalChange, disabled,
}: {
  partyId: string;
  isGlobalSupplier: boolean;
  onGlobalChange: (v: boolean) => void;
  disabled?: boolean;
}) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");

  const toggleGlobal = useMutation({
    mutationFn: (v: boolean) => setGlobalSupplier(partyId, v),
    onSuccess: (_d, v) => { onGlobalChange(v); toast.success(tc("feedback.updated")); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  return (
    <div className="space-y-6">
      <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
        <Switch checked={isGlobalSupplier} disabled={disabled || toggleGlobal.isPending}
          onCheckedChange={(v) => toggleGlobal.mutate(v)} />
        <Building2 className="size-4 text-[var(--color-muted-foreground)]" />
        {t("parties.supplier.global")}
      </label>
      <p className="-mt-4 text-[11.5px] text-[var(--color-muted-foreground)]">{t("parties.supplier.globalHint")}</p>

      <MappingSection
        title={t("parties.supplier.brands")} icon={Tag} disabled={disabled} queryKey={["supplier-brands", partyId]}
        loadAssigned={() => getSupplierBrands(partyId)}
        loadOptions={async () => (await getGlobalBrands()).map((b) => ({ value: b.id, label: b.name }))}
        save={(ids) => setSupplierBrands(partyId, ids)}
        empty={t("parties.supplier.brandsEmpty")} choose={t("parties.supplier.chooseBrand")} />

      <MappingSection
        title={t("parties.supplier.categories")} icon={Tag} disabled={disabled} queryKey={["supplier-categories", partyId]}
        loadAssigned={() => getSupplierCategories(partyId)}
        loadOptions={async (s) => (await getGlobalCategories(s)).map((c) => ({ value: c.id, label: c.fullPath ?? c.name }))}
        save={(ids) => setSupplierCategories(partyId, ids)}
        empty={t("parties.supplier.categoriesEmpty")} choose={t("parties.supplier.chooseCategory")} searchable />
    </div>
  );
}

type Opt = { value: string; label: string };

function MappingSection({
  title, icon: Icon, disabled, queryKey, loadAssigned, loadOptions, save, empty, choose, searchable,
}: {
  title: string;
  icon: typeof Tag;
  disabled?: boolean;
  queryKey: unknown[];
  loadAssigned: () => Promise<string[]>;
  loadOptions: (search?: string) => Promise<Opt[]>;
  save: (ids: string[]) => Promise<void>;
  empty: string;
  choose: string;
  searchable?: boolean;
}) {
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const [picked, setPicked] = useState<string | null>(null);

  const assignedQ = useQuery({ queryKey: ["catalog", ...queryKey], queryFn: loadAssigned });
  const optionsQ = useQuery({ queryKey: ["catalog", ...queryKey, "options"], queryFn: () => loadOptions() });

  const labelOf = useMemo(() => {
    const m = new Map<string, string>();
    for (const o of optionsQ.data ?? []) m.set(o.value, o.label);
    return m;
  }, [optionsQ.data]);

  const assigned = assignedQ.data ?? [];
  const available = useMemo(
    () => (optionsQ.data ?? []).filter((o) => !assigned.includes(o.value)),
    [optionsQ.data, assigned],
  );

  const mutate = useMutation({
    mutationFn: (ids: string[]) => save(ids),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["catalog", ...queryKey] }),
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });

  const add = () => { if (picked) { mutate.mutate([...assigned, picked]); setPicked(null); } };
  const remove = (id: string) => mutate.mutate(assigned.filter((x) => x !== id));

  return (
    <section className="border-t border-[var(--color-border)] pt-4">
      <h3 className="mb-2 text-sm font-semibold text-[var(--color-foreground)]">{title}</h3>
      {!disabled && (
        <div className="mb-2 flex items-end gap-2">
          <div className="grow">
            <Combobox id={`map-${queryKey.join("-")}`} label={title} value={picked} onChange={setPicked}
              options={available} searchable={searchable} clearable placeholder={choose} />
          </div>
          <Button type="button" size="sm" disabled={!picked || mutate.isPending} onClick={add}>
            <Plus className="size-4" />{tc("actions.add", "Agregar")}
          </Button>
        </div>
      )}
      <div className="flex flex-wrap gap-1.5">
        {assigned.map((id) => (
          <span key={id} className="inline-flex items-center gap-1.5 rounded-full border border-[var(--color-border)] bg-[var(--color-muted)] px-2.5 py-1 text-[12px] text-[var(--color-foreground)]">
            <Icon className="size-3" />{labelOf.get(id) ?? id}
            {!disabled && (
              <button type="button" onClick={() => remove(id)} className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-3" /></button>
            )}
          </span>
        ))}
        {assigned.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{empty}</p>}
      </div>
    </section>
  );
}
