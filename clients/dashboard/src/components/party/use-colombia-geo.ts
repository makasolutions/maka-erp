import { useQuery } from "@tanstack/react-query";
import { getDepartments, type Department, type Municipality } from "@/api/geography";

/**
 * Loads the DIVIPOLA (DANE) departments → municipalities dataset from the API
 * (global reference data, cached indefinitely). Replaces the old static co-geo.json.
 */
export function useColombiaGeo() {
  const { data } = useQuery<Department[]>({
    queryKey: ["geo", "departments"],
    queryFn: getDepartments,
    staleTime: Infinity,
    gcTime: Infinity,
  });

  const departments = data ?? [];
  const municipalitiesOf = (deptCode: string | null | undefined): Municipality[] =>
    (deptCode && departments.find((d) => d.code === deptCode)?.municipalities) || [];

  const departmentByCode = (code: string | null | undefined): Department | undefined =>
    code ? departments.find((d) => d.code === code) : undefined;

  /** Find the department that owns a municipality code (5-digit). */
  const departmentOfMunicipality = (munCode: string | null | undefined): Department | undefined =>
    munCode ? departments.find((d) => d.municipalities.some((m) => m.code === munCode)) : undefined;

  return { departments, municipalitiesOf, departmentByCode, departmentOfMunicipality };
}
