import { useTranslation } from "react-i18next";
import { Plus, Star, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import type { PartyChannel } from "@/api/parties";

export interface ChannelEditorProps {
  value: PartyChannel[];
  onChange: (next: PartyChannel[]) => void;
  disabled?: boolean;
}

export function ChannelEditor({ value, onChange, disabled }: ChannelEditorProps) {
  const { t } = useTranslation("crm");
  const update = (i: number, patch: Partial<PartyChannel>) =>
    onChange(value.map((c, idx) => (idx === i ? { ...c, ...patch } : c)));
  const add = () => onChange([...value, { channelTypeCode: "", value: "", isPrimary: value.length === 0 }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));
  const makePrimary = (i: number) => onChange(value.map((c, idx) => ({ ...c, isPrimary: idx === i })));

  return (
    <div className="space-y-2">
      {value.length === 0 && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.channel.empty")}</p>}
      {value.map((c, i) => (
        <div key={i} className="flex items-end gap-2">
          <div className="w-40">
            <BasicRecordSelect id={`ch-type-${i}`} tableCode="PartyChannelType" label={t("parties.channel.type")}
              value={c.channelTypeCode || null} onChange={(v) => update(i, { channelTypeCode: v ?? "" })} disabled={disabled} />
          </div>
          <Input className="flex-1" value={c.value} disabled={disabled} placeholder={t("parties.channel.value")}
            onChange={(e) => update(i, { value: e.target.value })} />
          <Input className="w-40" value={c.reference ?? ""} disabled={disabled} placeholder={t("parties.channel.reference")}
            onChange={(e) => update(i, { reference: e.target.value })} />
          <button type="button" disabled={disabled} onClick={() => makePrimary(i)} aria-label={t("parties.channel.makePrimary")}
            className={`pb-2 ${c.isPrimary ? "text-[var(--color-warning)]" : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"}`}>
            <Star className="size-4" fill={c.isPrimary ? "currentColor" : "none"} />
          </button>
          <button type="button" disabled={disabled} onClick={() => remove(i)} aria-label={t("common:actions.delete", "Eliminar")}
            className="pb-2 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={add}><Plus className="size-4" />{t("parties.channel.add")}</Button>
      )}
    </div>
  );
}
