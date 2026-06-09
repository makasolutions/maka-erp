/**
 * TenantBrandingProvider — applies the operator-configured tenant branding
 * (Multitenancy `tenants/theme`) on sign-in: palette → CSS tokens, typography →
 * --font-sans, favicon, and exposes the brand logo via context for the chrome.
 *
 * This intentionally OVERRIDES the tenant's self-service appearance
 * (identity/appearance) when the operator has set a custom theme (isDefault=false),
 * per the product decision: the operator's branding wins. When the operator theme
 * is the framework default, nothing is overridden and the self-service appearance
 * rules as before.
 *
 * Must render inside AuthProvider (useAuth) and ThemeProvider (useTheme).
 */
import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/auth/use-auth";
import { useTheme } from "@/components/theme/theme-provider";
import { getCurrentTenantBranding, type TenantBrandingDto, type TenantPalette } from "@/api/branding";

type BrandingContextValue = {
  /** Logo URL resolved for the current theme, or null when none configured. */
  logoUrl: string | null;
};

const BrandingContext = createContext<BrandingContextValue>({ logoUrl: null });

// CSS custom properties we override from the operator palette. `--color-*`
// tokens reference these `--<token>` vars, so an inline override cascades.
const PALETTE_VARS = [
  "--primary", "--primary-foreground", "--ring",
  "--background", "--card", "--popover",
  "--foreground", "--destructive", "--warning", "--success", "--info",
] as const;

function applyPalette(p: TenantPalette) {
  const root = document.documentElement;
  root.style.setProperty("--primary", p.primary);
  root.style.setProperty("--primary-foreground", p.surface);
  root.style.setProperty("--ring", p.primary);
  root.style.setProperty("--background", p.background);
  root.style.setProperty("--card", p.surface);
  root.style.setProperty("--popover", p.surface);
  root.style.setProperty("--foreground", p.secondary);
  root.style.setProperty("--destructive", p.error);
  root.style.setProperty("--warning", p.warning);
  root.style.setProperty("--success", p.success);
  root.style.setProperty("--info", p.info);
}

function clearPalette() {
  const root = document.documentElement;
  for (const v of PALETTE_VARS) root.style.removeProperty(v);
}

function applyFavicon(href: string | null) {
  if (!href) return;
  let link = document.querySelector<HTMLLinkElement>('link[rel~="icon"]');
  if (!link) {
    link = document.createElement("link");
    link.rel = "icon";
    document.head.appendChild(link);
  }
  link.href = href;
}

export function TenantBrandingProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const { resolved } = useTheme();
  const [logoUrl, setLogoUrl] = useState<string | null>(null);

  const { data } = useQuery({
    queryKey: ["tenant", "branding", "current"],
    queryFn: getCurrentTenantBranding,
    enabled: isAuthenticated,
    staleTime: 30 * 60 * 1000,
    retry: 1,
  });

  const active: TenantBrandingDto | null =
    isAuthenticated && data && !data.isDefault ? data : null;

  // Palette + typography — re-applied when the resolved (light/dark) theme flips.
  useEffect(() => {
    if (!active) {
      clearPalette();
      return;
    }
    applyPalette(resolved === "dark" ? active.darkPalette : active.lightPalette);
    const font = active.typography?.fontFamily?.trim();
    if (font) document.documentElement.style.setProperty("--font-sans", font);
  }, [active, resolved]);

  // Brand assets — favicon + logo (logo resolved per theme).
  useEffect(() => {
    if (!active) {
      setLogoUrl(null);
      return;
    }
    applyFavicon(active.brandAssets.faviconUrl ?? null);
    const dark = resolved === "dark";
    const logo = (dark ? active.brandAssets.logoDarkUrl : active.brandAssets.logoUrl)
      ?? active.brandAssets.logoUrl ?? null;
    setLogoUrl(logo);
  }, [active, resolved]);

  // Clear overrides on sign-out so the next user doesn't inherit this branding.
  useEffect(() => {
    if (!isAuthenticated) {
      clearPalette();
      setLogoUrl(null);
    }
  }, [isAuthenticated]);

  const value = useMemo<BrandingContextValue>(() => ({ logoUrl }), [logoUrl]);
  return <BrandingContext.Provider value={value}>{children}</BrandingContext.Provider>;
}

export function useBranding(): BrandingContextValue {
  return useContext(BrandingContext);
}
