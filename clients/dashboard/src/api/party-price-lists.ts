import { apiFetch } from "@/lib/api-client";
import type { PriceListKind } from "@/api/catalog";

export type PartyPriceListDto = {
  id: string;
  partyId: string;
  priceListId: string;
  priceListName: string;
  listKind: PriceListKind;
  isActive: boolean;
  validFrom?: string | null;
  validTo?: string | null;
};

export function getPartyPriceLists(partyId: string): Promise<PartyPriceListDto[]> {
  return apiFetch<PartyPriceListDto[]>(`/api/v1/catalog/party-price-lists/${partyId}`);
}

export function assignPartyPriceList(
  partyId: string, priceListId: string, validFrom?: string | null, validTo?: string | null,
): Promise<string> {
  return apiFetch<string>(`/api/v1/catalog/party-price-lists`, {
    method: "POST",
    body: JSON.stringify({ partyId, priceListId, validFrom: validFrom ?? null, validTo: validTo ?? null }),
  });
}

export async function removePartyPriceList(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/catalog/party-price-lists/${id}`, { method: "DELETE" });
}
