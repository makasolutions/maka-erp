import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DollarSign, Eye, Pencil, Plus, Tag } from "lucide-react";
import { toast } from "sonner";
import {
  addPriceListItem, createPriceList, getPriceListById, getPriceLists, getVariations,
  searchProducts, updatePriceListItem,
  type PriceListDto, type PriceListItemDto,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import {
  Combobox, EntityFilterPill, EntityPageHeader, EntityStatusBadge, Field, FormGrid,
} from "@/components/list";
import { MakaFilterField, MakaFilterInput, MakaGridClient, MakaGridFilters } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe, formatMoney } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const SEGMENTS = ["Retail", "B2B", "VIP", "Mayorista"];
const LIST_KEY = ["catalog", "price-lists"] as const;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "detail"; list: PriceListDto };

type PriceListRow = PriceListDto & { segmentLabel: string; validityLabel: string };

function triToBool(v: string | null): boolean | undefined {
  return v === null ? undefined : v === "true";
}

function fmtDate(s?: string | null): string {
  if (!s) return "—";
  try { return new Date(s).toLocaleDateString("es-CO", { year: "numeric", month: "short", day: "numeric" }); }
  catch { return "—"; }
}

// ── Cell templates ──
function NameCell(row: PriceListRow) {
  return (
    <div className="min-w-0">
      <div className="truncate text-[13px] font-medium text-[var(--color-foreground)]">{row.name}</div>
      {row.description && (
        <div className="truncate text-[12px] text-[var(--color-muted-foreground)]" title={row.description}>{row.description}</div>
      )}
    </div>
  );
}
function SegmentCell(row: PriceListRow) {
  return <EntityStatusBadge tone="info">{row.segmentLabel}</EntityStatusBadge>;
}
function ItemsCell(row: PriceListRow) {
  return <span className="tabular-nums text-[13px] text-[var(--color-foreground)]">{row.itemCount}</span>;
}
function ValidityCell(row: PriceListRow) {
  return <span className="text-[12.5px] text-[var(--color-muted-foreground)]">{row.validityLabel}</span>;
}
function ActiveCell(row: PriceListRow) {
  return <EntityStatusBadge tone={row.isActive ? "success" : "default"}>{row.isActive ? "✓" : "—"}</EntityStatusBadge>;
}
function DefaultCell(row: PriceListRow) {
  if (row.isDefault) return <EntityStatusBadge tone="success">★</EntityStatusBadge>;
  if (row.adjustmentPercent != null) {
    const p = row.adjustmentPercent;
    return <span className={`text-[12.5px] font-medium tabular-nums ${p >= 0 ? "text-[var(--color-warning)]" : "text-[var(--color-success)]"}`}>{p > 0 ? "+" : ""}{p}%</span>;
  }
  return <span className="text-[12px] text-[var(--color-muted-foreground)]">—</span>;
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function PriceListsPage() {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const { can } = usePerm();

  const [panelOpen, setPanelOpen] = useState(true);
  const [nameFilter, setNameFilter] = useState("");
  const [segmentFilter, setSegmentFilter] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: [...LIST_KEY, "list"],
    queryFn: () => getPriceLists({ pageSize: 200, sort: "name", kind: "Segment" }),
    placeholderData: keepPreviousData,
  });

  const all = query.data?.items ?? [];
  const rows: PriceListRow[] = useMemo(() => {
    const name = nameFilter.trim().toLowerCase();
    const active = triToBool(activeFilter);
    return all
      .filter((p) =>
        (!name || p.name.toLowerCase().includes(name)) &&
        (!segmentFilter || p.customerSegment === segmentFilter) &&
        (active === undefined || p.isActive === active))
      .map((p) => ({
        ...p,
        segmentLabel: p.customerSegment,
        validityLabel: `${fmtDate(p.validFrom)} → ${p.validTo ? fmtDate(p.validTo) : "∞"}`,
      }));
  }, [all, nameFilter, segmentFilter, activeFilter]);

  const columns: ColumnModel[] = useMemo(() => [
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "name", headerText: t("priceLists.fields.name"), template: NameCell as any, minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "customerSegment", headerText: t("priceLists.fields.segment"), template: SegmentCell as any, width: 130, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "isDefault", headerText: t("priceLists.fields.defaultOrPercent"), template: DefaultCell as any, width: 120, textAlign: "Center", allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "itemCount", headerText: t("priceLists.fields.items"), template: ItemsCell as any, width: 90, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "validFrom", headerText: t("priceLists.fields.validity"), template: ValidityCell as any, minWidth: 180, allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "isActive", headerText: t("priceLists.fields.active"), template: ActiveCell as any, width: 90, textAlign: "Center", allowSorting: false },
  ], [t]);

  const resetFilters = () => { setNameFilter(""); setSegmentFilter(null); setActiveFilter(null); };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={DollarSign}
        title={t("priceLists.title")}
        total={query.data?.totalCount ?? null}
        unit={t("priceLists.singular")}
        description={t("priceLists.description")}
      >
        <Button variant="outline" onClick={() => setPanelOpen((v) => !v)} aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold">
          <Eye className="size-4" />{tc("gridFilters.panelToggle")}
        </Button>
        <Button perm={P.catalog.priceLists.manage} onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none">
          <Plus className="size-4" />{t("priceLists.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={resetFilters}
        filters={
          <>
            <MakaFilterField label={t("priceLists.fields.name")} className="grow">
              <MakaFilterInput value={nameFilter} onChange={setNameFilter}
                placeholder={t("priceLists.filters.namePlaceholder")} ariaLabel={t("priceLists.fields.name")} className="min-w-48" />
            </MakaFilterField>
            <MakaFilterField label={t("priceLists.fields.segment")}>
              <EntityFilterPill<string | null>
                label={t("priceLists.fields.segment")} value={segmentFilter} onChange={setSegmentFilter}
                options={[{ value: null, label: tc("status.all") }, ...SEGMENTS.map((s) => ({ value: s, label: s }))]} />
            </MakaFilterField>
            <MakaFilterField label={t("priceLists.fields.active")}>
              <EntityFilterPill<string | null>
                label={t("priceLists.fields.active")} value={activeFilter} onChange={setActiveFilter}
                options={[
                  { value: null, label: tc("status.all") },
                  { value: "true", label: tc("status.active") },
                  { value: "false", label: tc("status.inactive") },
                ]} />
            </MakaFilterField>
          </>
        }
      />

      <MakaGridClient<PriceListRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="listas-precios"
        entityName={t("priceLists.singular")}
        onRowClick={(row) => setEditor({ mode: "detail", list: row })}
        onClearFilters={resetFilters}
        permissions={{ edit: P.catalog.priceLists.view }}
        onEdit={(row) => setEditor({ mode: "detail", list: row })}
      />

      <CreatePriceListDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <PriceListDetailDialog state={editor} onClose={() => setEditor({ mode: "closed" })} canEdit={can(P.catalog.priceLists.manage)} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreatePriceListDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "create";

  const [name, setName] = useState("");
  const [segment, setSegment] = useState<string | null>("Retail");
  const [validFrom, setValidFrom] = useState("");
  const [validTo, setValidTo] = useState("");
  const [description, setDescription] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [adjustmentPercent, setAdjustmentPercent] = useState("");

  useEffect(() => {
    if (isOpen) { setName(""); setSegment("Retail"); setValidFrom(""); setValidTo(""); setDescription(""); setIsDefault(false); setAdjustmentPercent(""); }
  }, [isOpen]);

  const createM = useMutation({
    mutationFn: () => createPriceList({
      name: name.trim(),
      customerSegment: (segment ?? "Retail").trim(),
      validFrom: validFrom ? new Date(validFrom).toISOString() : null,
      validTo: validTo ? new Date(validTo).toISOString() : null,
      description: description.trim() || null,
      isDefault,
      adjustmentPercent: isDefault || !adjustmentPercent ? null : Number(adjustmentPercent),
    }),
    onSuccess: () => {
      toast.success(tc("feedback.created"));
      queryClient.invalidateQueries({ queryKey: LIST_KEY });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });

  const canSubmit = !!name.trim() && !!segment;
  const onSubmit = (e: FormEvent<HTMLFormElement>) => { e.preventDefault(); if (canSubmit) createM.mutate(); };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("priceLists.actions.create")}</DialogTitle>
            <DialogDescription>{t("priceLists.createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="pl-name" span={8} label={t("priceLists.fields.name")} required>
                <Input id="pl-name" value={name} onChange={(e) => setName(e.target.value)}
                  placeholder={t("priceLists.namePlaceholder")} autoFocus required maxLength={200} />
              </Field>
              <Field id="pl-segment" span={4} label={t("priceLists.fields.segment")} required hint={t("priceLists.segmentHint")}>
                <Combobox id="pl-segment" label={t("priceLists.fields.segment")} value={segment} onChange={setSegment}
                  options={SEGMENTS.map((s) => ({ value: s, label: s }))} searchable
                  onCreate={(q) => setSegment(q.trim())} createLabel={tc("actions.create")} />
              </Field>
              <Field id="pl-from" span={6} label={t("priceLists.fields.validFrom")} hint={t("priceLists.validFromHint")}>
                <Input id="pl-from" type="date" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} />
              </Field>
              <Field id="pl-to" span={6} label={t("priceLists.fields.validTo")} hint={t("priceLists.validToHint")}>
                <Input id="pl-to" type="date" value={validTo} onChange={(e) => setValidTo(e.target.value)} />
              </Field>
              <div className="col-span-1 flex flex-wrap items-center gap-6 sm:col-span-12">
                <label className="flex items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
                  <input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)}
                    className="size-4 accent-[var(--color-primary)]" />
                  {t("priceLists.fields.isDefault")}
                  <span className="text-[12px] font-normal text-[var(--color-muted-foreground)]">— {t("priceLists.isDefaultHint")}</span>
                </label>
              </div>
              {!isDefault && (
                <Field id="pl-pct" span={6} label={t("priceLists.fields.adjustmentPercent")} hint={t("priceLists.adjustmentHint")}>
                  <Input id="pl-pct" type="number" step="0.01" value={adjustmentPercent}
                    onChange={(e) => setAdjustmentPercent(e.target.value)} placeholder="-3 / 15" />
                </Field>
              )}
              <Field id="pl-desc" span={isDefault ? 12 : 6} label={t("priceLists.fields.description")}>
                <Input id="pl-desc" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={500} />
              </Field>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={createM.isPending}>{tc("actions.cancel")}</Button>
            </DialogClose>
            <Button type="submit" disabled={createM.isPending || !canSubmit}>
              {createM.isPending ? tc("feedback.saving") : t("priceLists.actions.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Detail dialog — items + add/edit
// ───────────────────────────────────────────────────────────────────────

function PriceListDetailDialog({ state, onClose, canEdit }: { state: EditorState; onClose: () => void; canEdit: boolean }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "detail";
  const list = state.mode === "detail" ? state.list : undefined;
  const listId = list?.id;

  const detailQ = useQuery({
    queryKey: ["catalog", "price-lists", "detail", listId],
    queryFn: () => getPriceListById(listId!),
    enabled: isOpen && !!listId,
  });
  const items = detailQ.data?.items ?? [];

  // Add-item form
  const productsQ = useQuery({
    queryKey: ["catalog", "products", "list-prices"],
    queryFn: () => searchProducts({ pageSize: 200, sort: "name" }),
    enabled: isOpen,
  });
  const [pickProductId, setPickProductId] = useState<string | null>(null);
  const [pickVariationId, setPickVariationId] = useState<string | null>(null);
  const [price, setPrice] = useState("");
  const [minQty, setMinQty] = useState("");
  const [salePrice, setSalePrice] = useState("");

  const variationsQ = useQuery({
    queryKey: ["catalog", "variations", pickProductId],
    queryFn: () => getVariations(pickProductId!),
    enabled: isOpen && !!pickProductId,
  });
  useEffect(() => {
    const vs = (variationsQ.data ?? []).filter((v) => !v.isDeleted);
    setPickVariationId(vs.length ? (vs.find((v) => v.isDefault)?.id ?? vs[0].id) : null);
  }, [variationsQ.data]);

  // Edit-price sub-state
  const [editItem, setEditItem] = useState<PriceListItemDto | null>(null);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["catalog", "price-lists", "detail", listId] });
    queryClient.invalidateQueries({ queryKey: LIST_KEY });
  };

  const addM = useMutation({
    mutationFn: () => addPriceListItem(listId!, {
      variationId: pickVariationId!,
      price: Number(price),
      minQuantity: minQty ? Number(minQty) : null,
      salePrice: salePrice ? Number(salePrice) : null,
    }),
    onSuccess: () => { setPrice(""); setMinQty(""); setSalePrice(""); invalidate(); },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });

  const productOptions = (productsQ.data?.items ?? []).map((p) => ({ value: p.id, label: p.name }));
  const variationOptions = (variationsQ.data ?? []).filter((v) => !v.isDeleted)
    .map((v) => ({ value: v.id, label: v.sku + (v.isDefault ? " ★" : "") }));
  const canAdd = !!pickVariationId && !!price && Number(price) > 0 && !addM.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{list?.name}</DialogTitle>
          <DialogDescription>
            {list?.customerSegment} · {t("priceLists.fields.items")}: {items.length}
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="space-y-5">
            {/* Items list */}
            {items.length === 0 ? (
              <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("priceLists.empty")}</p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {items.map((it) => (
                  <li key={it.id} className="flex items-center justify-between gap-3 px-3 py-2.5 text-[13px]">
                    <div className="flex min-w-0 items-center gap-3">
                      <code className="font-mono text-[var(--color-foreground)]">{it.variationSku ?? it.variationId.slice(0, 8)}</code>
                      <span className="font-semibold tabular-nums text-[var(--color-foreground)]">{formatMoney(it.price)}</span>
                      {it.salePrice != null && (
                        <span className="tabular-nums text-[var(--color-success)]">{t("priceLists.onSale")}: {formatMoney(it.salePrice)}</span>
                      )}
                      {it.minQuantity != null && (
                        <span className="text-[var(--color-muted-foreground)]">≥{it.minQuantity}</span>
                      )}
                    </div>
                    {canEdit && (
                      <button type="button" onClick={() => setEditItem(it)} aria-label={tc("actions.edit")}
                        className="text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"><Pencil className="size-4" /></button>
                    )}
                  </li>
                ))}
              </ul>
            )}

            {/* Add item */}
            {canEdit && !editItem && (
              <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] p-3">
                <div className="mb-2 flex items-center gap-2">
                  <Tag className="size-4 text-[var(--color-muted-foreground)]" />
                  <h3 className="text-[13px] font-semibold text-[var(--color-foreground)]">{t("priceLists.addItem")}</h3>
                </div>
                <FormGrid>
                  <Field id="pi-prod" span={4} label={t("priceLists.product")}>
                    <Combobox id="pi-prod" label={t("priceLists.product")} value={pickProductId} onChange={setPickProductId}
                      options={productOptions} searchable clearable placeholder={t("priceLists.selectProduct")} />
                  </Field>
                  <Field id="pi-var" span={4} label={t("priceLists.variation")}>
                    <Combobox id="pi-var" label={t("priceLists.variation")} value={pickVariationId} onChange={setPickVariationId}
                      options={variationOptions} searchable />
                  </Field>
                  <Field id="pi-price" span={4} label={t("priceLists.price")} required>
                    <Input id="pi-price" type="number" min={0} step="1" value={price} onChange={(e) => setPrice(e.target.value)} />
                  </Field>
                  <Field id="pi-min" span={6} label={t("priceLists.minQty")} hint={t("priceLists.minQtyHint")}>
                    <Input id="pi-min" type="number" min={0} step="1" value={minQty} onChange={(e) => setMinQty(e.target.value)} />
                  </Field>
                  <Field id="pi-sale" span={6} label={t("priceLists.salePrice")} hint={t("priceLists.salePriceHint")}>
                    <Input id="pi-sale" type="number" min={0} step="1" value={salePrice} onChange={(e) => setSalePrice(e.target.value)} />
                  </Field>
                </FormGrid>
                <div className="mt-3 flex justify-end">
                  <Button type="button" disabled={!canAdd} onClick={() => addM.mutate()}>
                    <Plus className="size-4" />{t("priceLists.addItem")}
                  </Button>
                </div>
              </div>
            )}

            {/* Edit price */}
            {canEdit && editItem && (
              <EditItemPrice listId={listId!} item={editItem} onDone={() => { setEditItem(null); invalidate(); }} onCancel={() => setEditItem(null)} />
            )}
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">{tc("actions.close")}</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EditItemPrice({ listId, item, onDone, onCancel }: {
  listId: string; item: PriceListItemDto; onDone: () => void; onCancel: () => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const [price, setPrice] = useState(String(item.price));
  const [reason, setReason] = useState("");

  const updM = useMutation({
    mutationFn: () => updatePriceListItem(listId, item.id, {
      price: Number(price),
      changeReason: reason.trim() || null,
      salePrice: item.salePrice ?? null,
    }),
    onSuccess: () => { toast.success(tc("feedback.updated")); onDone(); },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  const canSave = !!price && Number(price) > 0 && !updM.isPending;

  return (
    <div className="rounded-lg border border-[var(--color-primary)] bg-[var(--color-background)] p-3">
      <h3 className="mb-2 text-[13px] font-semibold text-[var(--color-foreground)]">
        {t("priceLists.editPrice")} · <code className="font-mono">{item.variationSku ?? item.variationId.slice(0, 8)}</code>
      </h3>
      <FormGrid>
        <Field id="ep-price" span={6} label={t("priceLists.price")} required>
          <Input id="ep-price" type="number" min={0} step="1" value={price} onChange={(e) => setPrice(e.target.value)} autoFocus />
        </Field>
        <Field id="ep-reason" span={6} label={t("priceLists.changeReason")} hint={t("priceLists.changeReasonHint")}>
          <Input id="ep-reason" value={reason} onChange={(e) => setReason(e.target.value)} maxLength={200} />
        </Field>
      </FormGrid>
      <div className="mt-3 flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={updM.isPending}>{tc("actions.cancel")}</Button>
        <Button type="button" disabled={!canSave} onClick={() => updM.mutate()}>
          {updM.isPending ? tc("feedback.saving") : tc("actions.saveChanges")}
        </Button>
      </div>
    </div>
  );
}
