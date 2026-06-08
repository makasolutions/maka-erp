import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type ScorecardStatus = "Borrador" | "Cerrado";
export type ScorecardGrade = "A" | "B" | "C" | "D" | "F";

export type ScorecardKpiDto = {
  id: string;
  code: string;
  name: string;
  weight: number;
  sortOrder: number;
  isActive: boolean;
};

export type ScorecardCriterionInput = {
  kpiCode: string;
  kpiName: string;
  weight: number;
  score: number;
  comment: string | null;
};
export type ScorecardCriterionDto = ScorecardCriterionInput & { id: string };

export type ScorecardDto = {
  id: string;
  supplierId: string;
  supplierName?: string | null;
  periodLabel: string;
  periodStart: string;
  status: ScorecardStatus;
  weightedScore: number;
  grade: ScorecardGrade;
};

export type ScorecardDetailDto = {
  id: string;
  supplierId: string;
  supplierName?: string | null;
  periodLabel: string;
  periodStart: string;
  status: ScorecardStatus;
  weightedScore: number;
  grade: ScorecardGrade;
  notes?: string | null;
  isMutable: boolean;
  criteria: ScorecardCriterionDto[];
};

export type SupplierRankingDto = {
  supplierId: string;
  supplierName?: string | null;
  periodLabel: string;
  weightedScore: number;
  grade: ScorecardGrade;
};

export type ScoreTrendPointDto = { periodLabel: string; periodStart: string; weightedScore: number };

export type ScorecardWriteInput = {
  supplierId: string;
  periodLabel: string;
  periodStart: string;
  notes: string | null;
  criteria: ScorecardCriterionInput[];
};

const KPI = "/api/v1/catalog/scorecard-kpis";
const SC = "/api/v1/catalog/supplier-scorecards";

// ── KPI catalog ──
export const getScorecardKpis = (activeOnly = false): Promise<ScorecardKpiDto[]> =>
  apiFetch(`${KPI}?ActiveOnly=${activeOnly}`);
export const upsertScorecardKpi = (input: {
  id: string | null; code: string; name: string; weight: number; sortOrder: number; isActive: boolean;
}): Promise<string> => apiFetch(KPI, { method: "POST", body: JSON.stringify(input) });
export const deleteScorecardKpi = (id: string): Promise<void> => apiFetch(`${KPI}/${id}`, { method: "DELETE" });
export const seedDefaultKpis = (): Promise<number> => apiFetch(`${KPI}/seed-defaults`, { method: "POST" });

// ── Scorecards ──
export function getScorecards(params: {
  search?: string | null; supplierId?: string | null; status?: ScorecardStatus | null;
  pageNumber?: number; pageSize?: number; sort?: string | null;
}): Promise<PagedResponse<ScorecardDto>> {
  const qs = new URLSearchParams();
  if (params.search) qs.set("Search", params.search);
  if (params.supplierId) qs.set("SupplierId", params.supplierId);
  if (params.status) qs.set("Status", params.status);
  qs.set("PageNumber", String(params.pageNumber ?? 1));
  qs.set("PageSize", String(params.pageSize ?? 50));
  if (params.sort) qs.set("Sort", params.sort);
  return apiFetch(`${SC}?${qs.toString()}`);
}
export const getScorecardById = (id: string): Promise<ScorecardDetailDto> => apiFetch(`${SC}/${id}`);
export const createScorecard = (input: ScorecardWriteInput): Promise<string> =>
  apiFetch(SC, { method: "POST", body: JSON.stringify(input) });
export const updateScorecard = (id: string, input: ScorecardWriteInput): Promise<void> =>
  apiFetch(`${SC}/${id}`, { method: "PUT", body: JSON.stringify({ id, ...input }) });
export const closeScorecard = (id: string): Promise<void> => apiFetch(`${SC}/${id}/close`, { method: "POST" });
export const deleteScorecard = (id: string): Promise<void> => apiFetch(`${SC}/${id}`, { method: "DELETE" });
export const getSupplierRanking = (): Promise<SupplierRankingDto[]> => apiFetch(`${SC}/ranking`);
export const getSupplierScoreTrend = (supplierId: string): Promise<ScoreTrendPointDto[]> =>
  apiFetch(`${SC}/trend/${supplierId}`);

/** Weighted score mirror of the backend (Σ score·weight / Σ weight, 1–5). */
export function computeWeightedScore(criteria: { weight: number; score: number }[]): number {
  const totalWeight = criteria.reduce((s, c) => s + c.weight, 0);
  if (totalWeight <= 0) return 0;
  return Math.round((criteria.reduce((s, c) => s + c.score * c.weight, 0) / totalWeight) * 100) / 100;
}
export function gradeFor(score: number): ScorecardGrade {
  if (score >= 4.5) return "A";
  if (score >= 3.5) return "B";
  if (score >= 2.5) return "C";
  if (score >= 1.5) return "D";
  return "F";
}
