import { useTranslation } from "react-i18next";
import { Plus, Trash2, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import type { PartyContact } from "@/api/parties";

export interface ContactEditorProps {
  value: PartyContact[];
  onChange: (next: PartyContact[]) => void;
  disabled?: boolean;
}

export function ContactEditor({ value, onChange, disabled }: ContactEditorProps) {
  const { t } = useTranslation("crm");
  const update = (i: number, patch: Partial<PartyContact>) =>
    onChange(value.map((c, idx) => (idx === i ? { ...c, ...patch } : c)));
  const add = () => onChange([...value, { reference: "", isCommercial: false }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      {value.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.contact.empty")}</p>}
      {value.map((c, i) => (
        <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
              <UserRound className="size-3.5" />{c.fullName?.trim() || `${t("parties.contact.singular")} ${i + 1}`}
            </span>
            <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
              className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <Input value={c.reference} disabled={disabled} placeholder={t("parties.contact.reference")}
              onChange={(e) => update(i, { reference: e.target.value })} />
            <Input value={c.fullName ?? ""} disabled={disabled} placeholder={t("parties.contact.fullName")}
              onChange={(e) => update(i, { fullName: e.target.value })} />
            <Input value={c.email ?? ""} disabled={disabled} placeholder={t("parties.contact.email")}
              onChange={(e) => update(i, { email: e.target.value })} />
            <Input value={c.cell ?? ""} disabled={disabled} placeholder={t("parties.contact.cell")}
              onChange={(e) => update(i, { cell: e.target.value })} />
          </div>
          <label className="mt-2 flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
            <Switch checked={c.isCommercial} disabled={disabled} onCheckedChange={(v) => update(i, { isCommercial: v })} />
            {t("parties.contact.isCommercial")}
          </label>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.contact.add")}</Button>
      )}
    </div>
  );
}
