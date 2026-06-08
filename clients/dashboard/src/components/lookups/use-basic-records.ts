import { useQuery } from "@tanstack/react-query";
import { getBasicRecordsByCode, type BasicRecordBrief } from "@/api/lookups";

/**
 * Loads the active records of a basic table by its code (e.g. "IdentificationType").
 * Reference data changes rarely, so it's cached aggressively. Used by every dropdown
 * that is backed by a Tabla Básica.
 */
export function useBasicRecords(tableCode: string | undefined) {
  return useQuery<BasicRecordBrief[]>({
    queryKey: ["lookups", "records", tableCode],
    queryFn: () => getBasicRecordsByCode(tableCode!),
    enabled: !!tableCode,
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  });
}
