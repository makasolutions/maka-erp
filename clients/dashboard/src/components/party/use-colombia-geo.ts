import { useQuery } from "@tanstack/react-query";

type GeoMap = Record<string, string[]>;

/** Loads the Colombian departments → cities dataset (public/data/co-geo.json), cached. */
export function useColombiaGeo() {
  const { data } = useQuery<GeoMap>({
    queryKey: ["geo", "co"],
    queryFn: async () => {
      const r = await fetch("/data/co-geo.json");
      if (!r.ok) throw new Error("geo load failed");
      return r.json() as Promise<GeoMap>;
    },
    staleTime: Infinity,
    gcTime: Infinity,
  });

  const departments = data ? Object.keys(data) : [];
  const citiesOf = (department: string | null | undefined) =>
    department && data?.[department] ? data[department] : [];

  /** All cities flattened (for the city-first selector). */
  const allCities = data ? Object.values(data).flat() : [];

  /** Reverse lookup: which department a city belongs to (city-first cascade). */
  const deptOfCity = (city: string | null | undefined): string | null => {
    if (!city || !data) return null;
    for (const [dept, cities] of Object.entries(data)) if (cities.includes(city)) return dept;
    return null;
  };

  return { departments, citiesOf, allCities, deptOfCity };
}
