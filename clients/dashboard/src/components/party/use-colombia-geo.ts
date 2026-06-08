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

  return { departments, citiesOf };
}
