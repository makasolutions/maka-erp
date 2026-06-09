import { useTranslation } from "react-i18next";
import { Plus, Star, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FormGrid } from "@/components/list";
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
    <div className="space-y-3">
      {value.length === 0 && (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.channel.empty")}</p>
      )}
      {value.map((c, i) => (
        <div key={i} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-end gap-1">
            <button type="button" disabled={disabled} onClick={() => makePrimary(i)}
              aria-label={t("parties.channel.makePrimary")} title={t("parties.channel.makePrimary")}
              className={`rounded p-1 ${c.isPrimary ? "text-[var(--color-warning)]" : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"}`}>
              <Star className="size-4" fill={c.isPrimary ? "currentColor" : "none"} />
            </button>
            <button type="button" disabled={disabled} onClick={() => remove(i)}
              aria-label={t("common:actions.delete", "Eliminar")}
              className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
              <Trash2 className="size-4" />
            </button>
          </div>
          <FormGrid>
            <Field id={`ch-type-${i}`} span={4} label={t("parties.channel.type")}>
              <BasicRecordSelect id={`ch-type-${i}`} tableCode="PartyChannelType" label={t("parties.channel.type")}
                value={c.channelTypeCode || null} onChange={(v) => update(i, { channelTypeCode: v ?? "" })} disabled={disabled} />
            </Field>
            <Field id={`ch-value-${i}`} span={4} label={t("parties.channel.value")}>
              <Input id={`ch-value-${i}`} value={c.value} disabled={disabled} maxLength={256}
                onChange={(e) => update(i, { value: e.target.value })} />
            </Field>
            <Field id={`ch-ref-${i}`} span={4} label={t("parties.channel.reference")}>
              <Input id={`ch-ref-${i}`} value={c.reference ?? ""} disabled={disabled} maxLength={128}
                onChange={(e) => update(i, { reference: e.target.value })} />
            </Field>
          </FormGrid>
        </div>
      ))}
      {!disabled && (
        <Button type="button" variant="outline" size="sm" className="w-full sm:w-auto" onClick={add}>
          <Plus className="size-4" />{t("parties.channel.add")}
        </Button>
      )}
    </div>
  );
}
