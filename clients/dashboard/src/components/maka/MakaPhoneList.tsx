import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Phone as PhoneIcon, Plus, Save, Star, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Field, FormGrid } from "@/components/list";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import {
  type PhoneDto, type PhoneWriteInput, createPhone, deletePhone, emptyPhoneInput,
  listPhones, phonesQueryKey, setPrimaryPhone, updatePhone,
} from "@/api/phones";

/** Fila editable: un teléfono persistido (id) o un borrador (id = null). */
type Row = PhoneWriteInput & { id: string | null };

const toRow = (p: PhoneDto): Row => ({
  id: p.id, typeCode: p.typeCode, isActive: p.isActive, isPrimary: p.isPrimary,
  number: p.number, extension: p.extension, countryCode: p.countryCode,
});

export interface MakaPhoneListProps {
  /** Tipo del owner (p.ej. "Party", "Employee"). Asociación polimórfica. */
  ownerType: string;
  /** Id del owner. */
  ownerId: string;
  disabled?: boolean;
}

/**
 * Control genérico reutilizable de teléfonos (PR-G2). CRUD autónomo contra el módulo
 * SharedRecords vía OwnerType + OwnerId. Montable en cualquier entidad:
 * <code>&lt;MakaPhoneList ownerType="Party" ownerId={id} /&gt;</code>.
 */
export function MakaPhoneList({ ownerType, ownerId, disabled }: MakaPhoneListProps) {
  const { t } = useTranslation("common");
  const qc = useQueryClient();
  const queryKey = phonesQueryKey(ownerType, ownerId);

  const { data, isLoading } = useQuery({
    queryKey,
    queryFn: () => listPhones(ownerType, ownerId),
    enabled: Boolean(ownerType && ownerId),
  });

  const [rows, setRows] = useState<Row[]>([]);
  // `data` solo cambia tras una mutación → re-sembrar es seguro (los borradores sobreviven hasta guardar).
  useEffect(() => { setRows((data ?? []).map(toRow)); }, [data]);

  const invalidate = () => qc.invalidateQueries({ queryKey });
  const onError = (e: unknown) => toast.error(e instanceof Error ? e.message : t("phone.saveError"));

  const createMut = useMutation({
    mutationFn: (row: Row) => createPhone(ownerType, ownerId, stripId(row)),
    onSuccess: () => { toast.success(t("phone.saved")); invalidate(); }, onError,
  });
  const updateMut = useMutation({
    mutationFn: (row: Row) => updatePhone(row.id!, stripId(row)),
    onSuccess: () => { toast.success(t("phone.saved")); invalidate(); }, onError,
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => deletePhone(id),
    onSuccess: () => { toast.success(t("phone.deleted")); invalidate(); }, onError,
  });
  const primaryMut = useMutation({
    mutationFn: (id: string) => setPrimaryPhone(id),
    onSuccess: invalidate, onError,
  });

  const busy = disabled || createMut.isPending || updateMut.isPending || deleteMut.isPending || primaryMut.isPending;

  const patch = (i: number, p: Partial<Row>) => setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...p } : r)));
  const addDraft = () => setRows((rs) => [...rs, { ...emptyPhoneInput(rs.length === 0), id: null }]);
  const removeRow = (i: number) => {
    const row = rows[i];
    if (row.id) deleteMut.mutate(row.id);
    else setRows((rs) => rs.filter((_, idx) => idx !== i));
  };
  const saveRow = (i: number) => {
    const row = rows[i];
    if (!row.number || !row.number.trim()) { toast.error(t("phone.numberRequired")); return; }
    if (row.id) updateMut.mutate(row); else createMut.mutate(row);
  };
  const makePrimary = (i: number) => {
    const row = rows[i];
    if (row.id) primaryMut.mutate(row.id);
    else setRows((rs) => rs.map((r, idx) => ({ ...r, isPrimary: idx === i })));
  };

  return (
    <div className="space-y-3">
      {isLoading && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("phone.loading")}</p>}
      {!isLoading && rows.length === 0 && (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("phone.empty")}</p>
      )}

      {rows.map((p, i) => (
        <div key={p.id ?? `draft-${i}`} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
              <PhoneIcon className="size-3.5" />{`${t("phone.singular")} ${i + 1}`}
            </span>
            <button type="button" disabled={busy} onClick={() => removeRow(i)} aria-label={t("phone.delete")}
              className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
              <Trash2 className="size-4" />
            </button>
          </div>
          <FormGrid>
            <Field id={`phone-type-${i}`} span={4} label={t("phone.type")}>
              <BasicRecordSelect id={`phone-type-${i}`} tableCode="PhoneType" label={t("phone.type")}
                value={p.typeCode ?? null} onChange={(v) => patch(i, { typeCode: v })} disabled={busy} />
            </Field>
            <Field id={`phone-cc-${i}`} span={4} label={t("phone.countryCode")}>
              <Input id={`phone-cc-${i}`} value={p.countryCode ?? ""} disabled={busy} placeholder="+57"
                onChange={(e) => patch(i, { countryCode: e.target.value })} />
            </Field>
            <Field id={`phone-number-${i}`} span={4} label={t("phone.number")} required>
              <Input id={`phone-number-${i}`} value={p.number ?? ""} disabled={busy} placeholder="320 123 4567"
                onChange={(e) => patch(i, { number: e.target.value })} />
            </Field>
            <Field id={`phone-ext-${i}`} span={4} label={t("phone.extension")}>
              <Input id={`phone-ext-${i}`} value={p.extension ?? ""} disabled={busy} placeholder="101"
                onChange={(e) => patch(i, { extension: e.target.value })} />
            </Field>
            <Field id={`phone-active-${i}`} span={4} label={t("phone.active")}>
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={p.isActive} disabled={busy} onCheckedChange={(v) => patch(i, { isActive: v })} />
                {t("phone.active")}
              </label>
            </Field>
            <Field id={`phone-primary-${i}`} span={4} label={t("phone.primary")}>
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={p.isPrimary} disabled={busy} onCheckedChange={() => makePrimary(i)} />
                <Star className="size-3.5" fill={p.isPrimary ? "currentColor" : "none"} />{t("phone.primary")}
              </label>
            </Field>
          </FormGrid>
          <div className="mt-3 flex justify-end">
            <Button type="button" size="sm" onClick={() => saveRow(i)} disabled={busy}>
              <Save className="size-4" />{t("phone.save")}
            </Button>
          </div>
        </div>
      ))}

      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={addDraft} disabled={busy}>
          <Plus className="size-4" />{t("phone.add")}
        </Button>
      )}
    </div>
  );
}

const stripId = (row: Row): PhoneWriteInput => {
  const { id: _id, ...rest } = row;
  return rest;
};
