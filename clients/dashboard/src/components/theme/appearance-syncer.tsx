/**
 * AppearanceSyncer — thin component that lives inside AuthProvider + QueryClientProvider.
 *
 * Responsibilities:
 *  1. On mount (when authenticated): fetch GET /api/v1/identity/appearance.
 *  2. Apply the server config to the ThemeProvider DOM layer via applyFromApiConfig().
 *  3. Writes the API result to localStorage so the boot cache stays fresh.
 *
 * Why a separate component?
 *  ThemeProvider wraps AuthProvider (it must render first to avoid FOUC), so it
 *  cannot use useAuth(). AppearanceSyncer sits inside AuthProvider and bridges the gap.
 *  Saving is handled by the debounced schedulePersist() inside the ThemeProvider
 *  setters — no save logic needed here.
 *
 * Renders nothing — purely a side-effect component.
 */
import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/auth/use-auth";
import { getTenantAppearance } from "@/api/identity";
import { useTheme } from "@/components/theme/theme-provider";

export function AppearanceSyncer() {
  const { isAuthenticated } = useAuth();
  const { applyFromApiConfig } = useTheme();

  const { data } = useQuery({
    queryKey: ["identity", "appearance"],
    queryFn: getTenantAppearance,
    // Use the localStorage cache as placeholder for instant paint (no FOUC).
    placeholderData: undefined,
    // Re-fetch every 30 min — appearance rarely changes.
    staleTime: 30 * 60 * 1000,
    enabled: isAuthenticated,
    retry: 1,
  });

  // When the server config arrives, push it into the ThemeProvider DOM layer.
  useEffect(() => {
    if (data) {
      applyFromApiConfig(data);
    }
  }, [data, applyFromApiConfig]);

  return null;
}
