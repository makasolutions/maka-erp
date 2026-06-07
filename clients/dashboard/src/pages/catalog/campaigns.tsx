import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Megaphone, Plus, Trash2, XCircle } from "lucide-react";
import { toast } from "sonner";
import {
  cancelCampaign, createCampaign, getCampaigns, getDefaultVariation, getPriceListById,
  searchProducts, setCampaignItems,
  type CampaignStatus, type PriceListDto,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import {
  Dialog, DialogBody, DialogClose, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Combobox, EntityPageHeader, EntityStatusBadge, Field, FormGrid } from "@/components/list";
import { MakaDateRangePicker, MakaGridClient, type MakaDateRange } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { describe, formatMoney } from "@/lib/list-helpers";
import { usePerm } from "@/auth/permission-guard";
import { P } from "@/auth/permissions";

const CAMP_KEY = ["catalog", "campaigns"] as const;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "items"; campaign: PriceListDto }
  | { mode: "cancel"; campaign: PriceListDto };

type CampaignRow = PriceListDto & { windowLabel: string; statusLabel: string };

const STATUS_TONE: Record<CampaignStatus, "success" | "warning" | "default" | "danger"> = {
  Scheduled: "warning", Running: "success", Ended: "default", Cancelled: "danger",
};

function fmtDate(s?: string | null): string {
  if (!s) return "—";
  try { return new Date(s).toLocaleDateString("es-CO", { year: "numeric", month: "short", day: "numeric" }); }
  catch { return "—"; }
}

export function CampaignsPage() {
  const { t } = useTranslation("catalog");
  const { can } = usePerm();
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: [...CAMP_KEY, "list"],
    queryFn: () => getCampaigns(),
    placeholderData: keepPreviousData,
  });

  const rows: CampaignRow[] = useMemo(
    () => (query.data?.items ?? []).map((c) => ({
      ...c,
      windowLabel: `${fmtDate(c.validFrom)} → ${fmtDate(c.validTo)}`,
      statusLabel: c.campaignStatus ? t(`campaigns.status.${c.campaignStatus}`) : "—",
    })),
    [query.data, t],
  );

  const StatusCell = (row: CampaignRow) =>
    <EntityStatusBadge tone={row.campaignStatus ? STATUS_TONE[row.campaignStatus] : "default"}>{row.statusLabel}</EntityStatusBadge>;
  const WindowCell = (row: CampaignRow) =>
    <span className="text-[12.5px] text-[var(--color-muted-foreground)]">{row.windowLabel}</span>;
  const ItemsCell = (row: CampaignRow) =>
    <span className="tabular-nums text-[13px] text-[var(--color-foreground)]">{row.itemCount}</span>;

  const columns: ColumnModel[] = useMemo(() => [
    { field: "name", headerText: t("campaigns.fields.name"), minWidth: 220 },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "campaignStatus", headerText: t("campaigns.fields.status"), template: StatusCell as any, width: 130, textAlign: "Center", allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "validFrom", headerText: t("campaigns.fields.window"), template: WindowCell as any, minWidth: 200, allowSorting: false },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    { field: "itemCount", headerText: t("campaigns.fields.products"), template: ItemsCell as any, width: 100, textAlign: "Center" },
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ], [t]);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Megaphone}
        title={t("campaigns.title")}
        total={query.data?.totalCount ?? null}
        unit={t("campaigns.singular")}
        description={t("campaigns.description")}
      >
        <Button perm={P.catalog.priceLists.manage} onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none">
          <Plus className="size-4" />{t("campaigns.actions.create")}
        </Button>
      </EntityPageHeader>

      <MakaGridClient<CampaignRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isFetching}
        fileName="campanas"
        entityName={t("campaigns.singular")}
        onRowClick={(row) => setEditor({ mode: "items", campaign: row })}
        permissions={{ edit: P.catalog.priceLists.manage, delete: P.catalog.priceLists.manage }}
        onEdit={(row) => setEditor({ mode: "items", campaign: row })}
        onDelete={(row) => can(P.catalog.priceLists.manage) && setEditor({ mode: "cancel", campaign: row })}
      />

      <CreateCampaignDialog
        open={editor.mode === "create"}
        onClose={() => setEditor({ mode: "closed" })}
        onCreated={(campaign) => setEditor({ mode: "items", campaign })}
      />
      <CampaignItemsDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        canEdit={can(P.catalog.priceLists.manage)}
      />
      <CancelCampaignDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ── Create (name + validity window) ──
function CreateCampaignDialog({ open, onClose, onCreated }: {
  open: boolean; onClose: () => void; onCreated: (campaign: PriceListDto) => void;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [range, setRange] = useState<MakaDateRange | null>(null);
  const [description, setDescription] = useState("");
  useEffect(() => { if (open) { setName(""); setRange(null); setDescription(""); } }, [open]);

  const createM = useMutation({
    mutationFn: async () => {
      const id = await createCampaign({
        name: name.trim(),
        validFrom: range!.start.toISOString(),
        validTo: range!.end.toISOString(),
        description: description.trim() || null,
      });
      return id;
    },
    onSuccess: async (id) => {
      toast.success(tc("feedback.created"));
      await queryClient.invalidateQueries({ queryKey: CAMP_KEY });
      const list = await queryClient.fetchQuery({ queryKey: [...CAMP_KEY, "list"], queryFn: () => getCampaigns() });
      const created = list.items.find((c) => c.id === id);
      if (created) onCreated(created);
      else onClose();
    },
    onError: (e) => toast.error(tc("feedback.createFailed"), { description: describe(e) }),
  });

  const canSubmit = !!name.trim() && !!range;
  const onSubmit = (e: FormEvent<HTMLFormElement>) => { e.preventDefault(); if (canSubmit) createM.mutate(); };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{t("campaigns.actions.create")}</DialogTitle>
            <DialogDescription>{t("campaigns.createDesc")}</DialogDescription>
          </DialogHeader>
          <DialogBody>
            <FormGrid>
              <Field id="cp-name" span={12} label={t("campaigns.fields.name")} required>
                <Input id="cp-name" value={name} onChange={(e) => setName(e.target.value)}
                  placeholder={t("campaigns.namePlaceholder")} autoFocus required maxLength={128} />
              </Field>
              <Field id="cp-range" span={12} label={t("campaigns.fields.window")} required hint={t("campaigns.windowHint")}>
                <MakaDateRangePicker value={range} onChange={setRange} />
              </Field>
              <Field id="cp-desc" span={12} label={t("campaigns.fields.description")}>
                <Input id="cp-desc" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={500} />
              </Field>
            </FormGrid>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={createM.isPending}>{tc("actions.cancel")}</Button>
            </DialogClose>
            <Button type="submit" disabled={createM.isPending || !canSubmit}>
              {createM.isPending ? tc("feedback.saving") : t("campaigns.actions.next")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ── Product picker + campaign prices ──
type PickRow = { variationId: string; productName: string; sku: string; price: string };

function CampaignItemsDialog({ state, onClose, canEdit }: {
  state: EditorState; onClose: () => void; canEdit: boolean;
}) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "items";
  const campaign = state.mode === "items" ? state.campaign : undefined;
  const campaignId = campaign?.id;

  const detailQ = useQuery({
    queryKey: ["catalog", "campaign-detail", campaignId],
    queryFn: () => getPriceListById(campaignId!),
    enabled: isOpen && !!campaignId,
  });
  const productsQ = useQuery({
    queryKey: ["catalog", "products", "list-campaign"],
    queryFn: () => searchProducts({ pageSize: 200, sort: "name" }),
    enabled: isOpen,
  });

  const [rows, setRows] = useState<PickRow[]>([]);
  useEffect(() => {
    if (detailQ.data) {
      setRows(detailQ.data.items.map((it) => ({
        variationId: it.variationId, productName: it.variationSku ?? "", sku: it.variationSku ?? it.variationId.slice(0, 8), price: String(it.price),
      })));
    }
  }, [detailQ.data]);

  const [pickProductId, setPickProductId] = useState<string | null>(null);
  const productOptions = (productsQ.data?.items ?? []).map((p) => ({ value: p.id, label: p.name }));

  const addProduct = async () => {
    if (!pickProductId) return;
    const product = productsQ.data?.items.find((p) => p.id === pickProductId);
    const dv = await getDefaultVariation(pickProductId).catch(() => null);
    if (!dv) { toast.error(tc("feedback.createFailed")); return; }
    if (rows.some((r) => r.variationId === dv.id)) return;
    setRows((prev) => [...prev, { variationId: dv.id, productName: product?.name ?? "", sku: dv.sku, price: "" }]);
    setPickProductId(null);
  };

  const saveM = useMutation({
    mutationFn: () => setCampaignItems(campaignId!, rows
      .filter((r) => r.price && Number(r.price) >= 0)
      .map((r) => ({ variationId: r.variationId, price: Number(r.price) }))),
    onSuccess: () => {
      toast.success(tc("feedback.updated"));
      queryClient.invalidateQueries({ queryKey: CAMP_KEY });
      queryClient.invalidateQueries({ queryKey: ["catalog", "campaign-detail", campaignId] });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent size="form">
        <DialogHeader>
          <DialogTitle>{campaign?.name}</DialogTitle>
          <DialogDescription>{t("campaigns.itemsDesc")}</DialogDescription>
        </DialogHeader>
        <DialogBody>
          <div className="space-y-4">
            {canEdit && (
              <div className="flex items-end gap-2">
                <div className="grow">
                  <Combobox id="cp-prod" label={t("campaigns.product")} value={pickProductId} onChange={setPickProductId}
                    options={productOptions} searchable clearable placeholder={t("campaigns.selectProduct")} />
                </div>
                <Button type="button" variant="outline" disabled={!pickProductId} onClick={() => void addProduct()}>
                  <Plus className="size-4" />{tc("actions.add")}
                </Button>
              </div>
            )}

            {rows.length === 0 ? (
              <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("campaigns.noProducts")}</p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {rows.map((r, i) => (
                  <li key={r.variationId} className="flex items-center justify-between gap-3 px-3 py-2.5 text-[13px]">
                    <div className="min-w-0">
                      <div className="truncate text-[var(--color-foreground)]">{r.productName}</div>
                      <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">{r.sku}</code>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-40">
                        <Input type="number" min={0} step="1000" value={r.price} disabled={!canEdit}
                          placeholder={t("campaigns.price")}
                          onChange={(e) => setRows((prev) => prev.map((x, idx) => idx === i ? { ...x, price: e.target.value } : x))} />
                        {r.price && <p className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]">{formatMoney(Number(r.price))}</p>}
                      </div>
                      {canEdit && (
                        <button type="button" onClick={() => setRows((prev) => prev.filter((_, idx) => idx !== i))}
                          className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">{tc("actions.close")}</Button>
          </DialogClose>
          {canEdit && (
            <Button type="button" onClick={() => saveM.mutate()} disabled={saveM.isPending}>
              {saveM.isPending ? tc("feedback.saving") : t("campaigns.saveProducts")}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Cancel confirmation ──
function CancelCampaignDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const { t } = useTranslation("catalog");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const isOpen = state.mode === "cancel";
  const campaign = state.mode === "cancel" ? state.campaign : undefined;

  const cancelM = useMutation({
    mutationFn: (id: string) => cancelCampaign(id),
    onSuccess: () => {
      toast.success(t("campaigns.cancelled"));
      queryClient.invalidateQueries({ queryKey: CAMP_KEY });
      onClose();
    },
    onError: (e) => toast.error(tc("feedback.updateFailed"), { description: describe(e) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-[var(--color-destructive)]">
            <XCircle className="size-5" />{t("campaigns.actions.cancel")}
          </DialogTitle>
          <DialogDescription>{t("campaigns.cancelDesc", { name: campaign?.name ?? "" })}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={cancelM.isPending}>{tc("actions.cancel")}</Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => campaign && cancelM.mutate(campaign.id)} disabled={cancelM.isPending}>
            {cancelM.isPending ? tc("feedback.saving") : t("campaigns.actions.cancel")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
