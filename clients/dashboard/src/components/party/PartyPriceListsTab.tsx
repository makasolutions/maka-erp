import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Tag } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Combobox, EntityStatusBadge } from "@/components/list";
import { getPriceLists } from "@/api/catalog";
import {
  assignPartyPriceList, getPartyPriceLists, removePartyPriceList,
} from "@/api/party-price-lists";
import { describe } from "@/lib/list-helpers";

export function PartyPriceListsTab({ partyId, disabled }: { partyId: string; disabled?: boolean }) {
  const { t } = useTranslation("crm");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const [picked, setPicked] = useState<string | null>(null);

  const key = ["catalog", "party-price-lists", partyId] as const;
  const assignedQ = useQuery({ queryKey: key, queryFn: () => getPartyPriceLists(partyId) });
  const listsQ = useQuery({
    queryKey: ["catalog", "price-lists", "all"],
    queryFn: () => getPriceLists({ pageSize: 200, sort: "name" }),
  });

  const assignedIds = useMemo(() => new Set((assignedQ.data ?? []).map((a) => a.priceListId)), [assignedQ.data]);
  const options = useMemo(
    () => (listsQ.data?.items ?? [])
      .filter((l) => !assignedIds.has(l.id))
      .map((l) => ({ value: l.id, label: l.name })),
    [listsQ.data, assignedIds],
  );

  const assign = useMutation({
    mutationFn: () => assignPartyPriceList(partyId, picked!),
    onSuccess: () => { toast.success(tc("feedback.created")); setPicked(null); queryClient.invalidateQueries({ queryKey: key }); },
    onError: (e) => toast.error(tc("feedback.saveFailed"), { description: describe(e) }),
  });
  const remove = useMutation({
    mutationFn: (id: string) => removePartyPriceList(id),
    onSuccess: () => { toast.success(tc("feedback.deleted")); queryClient.invalidateQueries({ queryKey: key }); },
    onError: (e) => toast.error(tc("feedback.deleteFailed"), { description: describe(e) }),
  });

  return (
    <div className="space-y-3">
      <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.priceLists.hint")}</p>

      {!disabled && (
        <div className="flex items-end gap-2">
          <div className="grow">
            <Combobox id="ppl-add" label={t("parties.priceLists.list")} value={picked} onChange={setPicked}
              options={options} searchable clearable placeholder={t("parties.priceLists.choose")} />
          </div>
          <Button type="button" size="sm" disabled={!picked || assign.isPending} onClick={() => assign.mutate()}>
            <Plus className="size-4" />{t("parties.priceLists.add")}
          </Button>
        </div>
      )}

      <div className="space-y-2">
        {(assignedQ.data ?? []).map((a) => (
          <div key={a.id} className="flex items-center justify-between gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] px-3 py-2">
            <div className="flex items-center gap-2 text-[13px]">
              <Tag className="size-3.5 text-[var(--color-muted-foreground)]" />
              <span className="font-medium text-[var(--color-foreground)]">{a.priceListName}</span>
              <EntityStatusBadge tone="info">{t(`parties.priceLists.kind.${a.listKind}`, a.listKind)}</EntityStatusBadge>
              {!a.isActive && <EntityStatusBadge tone="default">{tc("status.inactive")}</EntityStatusBadge>}
            </div>
            {!disabled && (
              <button type="button" onClick={() => remove.mutate(a.id)} aria-label={tc("actions.delete")}
                className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"><Trash2 className="size-4" /></button>
            )}
          </div>
        ))}
        {(assignedQ.data ?? []).length === 0 && (
          <p className="text-[12.5px] text-[var(--color-muted-foreground)]">{t("parties.priceLists.empty")}</p>
        )}
      </div>
    </div>
  );
}
