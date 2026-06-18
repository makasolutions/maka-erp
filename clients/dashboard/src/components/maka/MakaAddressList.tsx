import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MapPin, Plus, Save, Star, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Field, FormGrid } from "@/components/list";
import { BasicRecordSelect } from "@/components/lookups/BasicRecordSelect";
import { CityPicker } from "@/components/party/CityPicker";
import {
  type AddressDto, type AddressWriteInput, addressesQueryKey, createAddress, deleteAddress,
  emptyAddressInput, listAddresses, setPrimaryAddress, updateAddress,
} from "@/api/addresses";

/** Fila editable: una dirección persistida (id) o un borrador (id = null). */
type Row = AddressWriteInput & { id: string | null };

const toRow = (a: AddressDto): Row => ({
  id: a.id, labelCode: a.labelCode, isActive: a.isActive, isPrimary: a.isPrimary, country: a.country,
  department: a.department, city: a.city, departmentCode: a.departmentCode, municipalityCode: a.municipalityCode,
  line: a.line, barrio: a.barrio, reference: a.reference, latitude: a.latitude, longitude: a.longitude,
});

export interface MakaAddressListProps {
  /** Tipo del owner (p.ej. "Party", "Employee"). Asociación polimórfica. */
  ownerType: string;
  /** Id del owner. */
  ownerId: string;
  disabled?: boolean;
}

/**
 * Control genérico reutilizable de direcciones (PR-G1). CRUD autónomo contra el módulo
 * SharedRecords vía OwnerType + OwnerId. Montable en cualquier entidad:
 * <code>&lt;MakaAddressList ownerType="Party" ownerId={id} /&gt;</code>.
 */
export function MakaAddressList({ ownerType, ownerId, disabled }: MakaAddressListProps) {
  const { t } = useTranslation("common");
  const qc = useQueryClient();
  const queryKey = addressesQueryKey(ownerType, ownerId);

  const { data, isLoading } = useQuery({
    queryKey,
    queryFn: () => listAddresses(ownerType, ownerId),
    enabled: Boolean(ownerType && ownerId),
  });

  const [rows, setRows] = useState<Row[]>([]);
  // `data` solo cambia tras una mutación → re-sembrar es seguro (no pisa ediciones locales,
  // que ocurren sin mutación). Los borradores (id=null) sobreviven hasta que se guardan.
  useEffect(() => { setRows((data ?? []).map(toRow)); }, [data]);

  const invalidate = () => qc.invalidateQueries({ queryKey });
  const onError = (e: unknown) => toast.error(e instanceof Error ? e.message : t("address.saveError"));

  const createMut = useMutation({
    mutationFn: (row: Row) => createAddress(ownerType, ownerId, stripId(row)),
    onSuccess: () => { toast.success(t("address.saved")); invalidate(); }, onError,
  });
  const updateMut = useMutation({
    mutationFn: (row: Row) => updateAddress(row.id!, stripId(row)),
    onSuccess: () => { toast.success(t("address.saved")); invalidate(); }, onError,
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteAddress(id),
    onSuccess: () => { toast.success(t("address.deleted")); invalidate(); }, onError,
  });
  const primaryMut = useMutation({
    mutationFn: (id: string) => setPrimaryAddress(id),
    onSuccess: invalidate, onError,
  });

  const busy = disabled || createMut.isPending || updateMut.isPending || deleteMut.isPending || primaryMut.isPending;

  const patch = (i: number, p: Partial<Row>) => setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...p } : r)));
  const addDraft = () => setRows((rs) => [...rs, { ...emptyAddressInput(rs.length === 0), id: null }]);
  const removeRow = (i: number) => {
    const row = rows[i];
    if (row.id) deleteMut.mutate(row.id);
    else setRows((rs) => rs.filter((_, idx) => idx !== i)); // borrador: descartar local
  };
  const saveRow = (i: number) => {
    const row = rows[i];
    if (!row.line || !row.line.trim()) { toast.error(t("address.lineRequired")); return; }
    if (row.id) updateMut.mutate(row); else createMut.mutate(row);
  };
  const makePrimary = (i: number) => {
    const row = rows[i];
    if (row.id) primaryMut.mutate(row.id); // persistida: coordinación en el servidor
    else setRows((rs) => rs.map((r, idx) => ({ ...r, isPrimary: idx === i }))); // borrador: local
  };

  return (
    <div className="space-y-3">
      {isLoading && <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("address.loading")}</p>}
      {!isLoading && rows.length === 0 && (
        <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("address.empty")}</p>
      )}

      {rows.map((a, i) => (
        <div key={a.id ?? `draft-${i}`} className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[12px] font-semibold text-[var(--color-muted-foreground)]">
              <MapPin className="size-3.5" />{`${t("address.singular")} ${i + 1}`}
            </span>
            <button type="button" disabled={busy} onClick={() => removeRow(i)} aria-label={t("address.delete")}
              className="rounded p-1 text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]">
              <Trash2 className="size-4" />
            </button>
          </div>
          <FormGrid>
            <Field id={`addr-label-${i}`} span={4} label={t("address.label")}>
              <BasicRecordSelect id={`addr-label-${i}`} tableCode="AddressLabel" label={t("address.label")}
                value={a.labelCode ?? null} onChange={(v) => patch(i, { labelCode: v })} disabled={busy} />
            </Field>
            <CityPicker idPrefix={`addr-geo-${i}`} disabled={busy}
              value={{ departmentCode: a.departmentCode, municipalityCode: a.municipalityCode, department: a.department, city: a.city }}
              onChange={(p) => patch(i, p)} />
            <Field id={`addr-line-${i}`} span={4} label={t("address.line")} required>
              <Input id={`addr-line-${i}`} value={a.line ?? ""} disabled={busy} placeholder="CL 100 # 13-21"
                onChange={(e) => patch(i, { line: e.target.value })} />
            </Field>
            <Field id={`addr-barrio-${i}`} span={4} label={t("address.barrio")}>
              <Input id={`addr-barrio-${i}`} value={a.barrio ?? ""} disabled={busy}
                onChange={(e) => patch(i, { barrio: e.target.value })} />
            </Field>
            <Field id={`addr-ref-${i}`} span={4} label={t("address.reference")}>
              <Input id={`addr-ref-${i}`} value={a.reference ?? ""} disabled={busy}
                onChange={(e) => patch(i, { reference: e.target.value })} />
            </Field>
            <Field id={`addr-lat-${i}`} span={4} label={t("address.lat")}>
              <Input id={`addr-lat-${i}`} type="number" step="0.0000001" min={-90} max={90} value={a.latitude ?? ""} disabled={busy}
                onChange={(e) => patch(i, { latitude: e.target.value === "" ? null : Number(e.target.value) })} />
            </Field>
            <Field id={`addr-lng-${i}`} span={4} label={t("address.lng")}>
              <Input id={`addr-lng-${i}`} type="number" step="0.0000001" min={-180} max={180} value={a.longitude ?? ""} disabled={busy}
                onChange={(e) => patch(i, { longitude: e.target.value === "" ? null : Number(e.target.value) })} />
            </Field>
            <Field id={`addr-active-${i}`} span={4} label={t("address.active")}>
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={a.isActive} disabled={busy} onCheckedChange={(v) => patch(i, { isActive: v })} />
                {t("address.active")}
              </label>
            </Field>
            <Field id={`addr-primary-${i}`} span={4} label={t("address.primary")}>
              <label className="flex h-9 items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                <Switch checked={a.isPrimary} disabled={busy} onCheckedChange={() => makePrimary(i)} />
                <Star className="size-3.5" fill={a.isPrimary ? "currentColor" : "none"} />{t("address.primary")}
              </label>
            </Field>
          </FormGrid>
          <div className="mt-3 flex justify-end">
            <Button type="button" size="sm" onClick={() => saveRow(i)} disabled={busy}>
              <Save className="size-4" />{t("address.save")}
            </Button>
          </div>
        </div>
      ))}

      {!disabled && (
        <Button type="button" variant="outline" size="sm" onClick={addDraft} disabled={busy}>
          <Plus className="size-4" />{t("address.add")}
        </Button>
      )}
    </div>
  );
}

const stripId = (row: Row): AddressWriteInput => {
  const { id: _id, ...rest } = row;
  return rest;
};
