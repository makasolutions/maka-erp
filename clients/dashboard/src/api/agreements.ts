import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type AgreementType = "ProveedorUnico" | "ProveedorModelo" | "DistribuidorAprobado" | "ModeloPendiente";
export type AgreementStatus = "Borrador" | "Vigente" | "Suspendido" | "Terminado";
export type AgreementResponsible = "Proveedor" | "Distribuidor" | "Plataforma";
export type AgreementRuleType =
  | "CompraMinimaMes" | "CompraMinimaAnio" | "AntiguedadMinimaMeses" | "VendeAEmpresa" | "VendeANatural"
  | "DocumentoExigido" | "CalificacionMinima" | "SlaEntregaDias" | "CumplimientoMinimo";
export type RuleEvaluationResult = "Cumple" | "NoCumple" | "Pendiente";

export type AgreementRuleInput = {
  ruleType: AgreementRuleType;
  numericValue: number | null;
  boolValue: boolean | null;
  textValue: string | null;
  isMandatory: boolean;
};

export type AgreementRuleDto = AgreementRuleInput & { id: string };

export type AgreementDto = {
  id: string;
  name: string;
  supplierId: string;
  supplierName?: string | null;
  agreementType: AgreementType;
  status: AgreementStatus;
  priceListId?: string | null;
  priceListName?: string | null;
  validFrom: string;
  validTo?: string | null;
  ruleCount: number;
};

export type AgreementDetailDto = {
  id: string;
  name: string;
  supplierId: string;
  supplierName?: string | null;
  agreementType: AgreementType;
  status: AgreementStatus;
  priceListId?: string | null;
  priceListName?: string | null;
  suggestedPriceListId?: string | null;
  suggestedPriceListName?: string | null;
  dispatchResponsible: AgreementResponsible;
  waybillResponsible: AgreementResponsible;
  settlementResponsible: AgreementResponsible;
  failedDeliveryPolicy?: string | null;
  returnsPolicy?: string | null;
  warrantyPolicy?: string | null;
  validFrom: string;
  validTo?: string | null;
  notes?: string | null;
  isMutable: boolean;
  rules: AgreementRuleDto[];
};

export type RuleEvaluationDto = {
  ruleType: AgreementRuleType;
  result: RuleEvaluationResult;
  isMandatory: boolean;
  detail: string;
};

export type AgreementEvaluationDto = {
  agreementId: string;
  distributorPartyId: string;
  distributorName?: string | null;
  eligible: boolean;
  rules: RuleEvaluationDto[];
};

export type AgreementWriteInput = {
  name: string;
  supplierId: string;
  agreementType: AgreementType;
  priceListId: string | null;
  suggestedPriceListId: string | null;
  dispatchResponsible: AgreementResponsible;
  waybillResponsible: AgreementResponsible;
  settlementResponsible: AgreementResponsible;
  failedDeliveryPolicy: string | null;
  returnsPolicy: string | null;
  warrantyPolicy: string | null;
  validFrom: string;
  validTo: string | null;
  notes: string | null;
  rules: AgreementRuleInput[];
};

const BASE = "/api/v1/catalog/agreements";

export function getAgreements(params: {
  search?: string | null;
  supplierId?: string | null;
  status?: AgreementStatus | null;
  pageNumber?: number;
  pageSize?: number;
  sort?: string | null;
}): Promise<PagedResponse<AgreementDto>> {
  const qs = new URLSearchParams();
  if (params.search) qs.set("Search", params.search);
  if (params.supplierId) qs.set("SupplierId", params.supplierId);
  if (params.status) qs.set("Status", params.status);
  qs.set("PageNumber", String(params.pageNumber ?? 1));
  qs.set("PageSize", String(params.pageSize ?? 50));
  if (params.sort) qs.set("Sort", params.sort);
  return apiFetch(`${BASE}?${qs.toString()}`);
}

export const getAgreementById = (id: string): Promise<AgreementDetailDto> => apiFetch(`${BASE}/${id}`);

export const createAgreement = (input: AgreementWriteInput): Promise<string> =>
  apiFetch(BASE, { method: "POST", body: JSON.stringify(input) });

export const updateAgreement = (id: string, input: AgreementWriteInput): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "PUT", body: JSON.stringify({ id, ...input }) });

export const changeAgreementStatus = (id: string, status: AgreementStatus): Promise<void> =>
  apiFetch(`${BASE}/${id}/status`, { method: "POST", body: JSON.stringify({ id, status }) });

export const deleteAgreement = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "DELETE" });

export const evaluateAgreement = (id: string, distributorId: string): Promise<AgreementEvaluationDto> =>
  apiFetch(`${BASE}/${id}/evaluate?distributorId=${distributorId}`);
