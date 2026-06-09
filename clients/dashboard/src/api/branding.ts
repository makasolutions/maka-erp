import { apiFetch } from "@/lib/api-client";

/**
 * Operator-configured tenant branding (Multitenancy `tenants/theme`). Read-only
 * for the current tenant via the self-service endpoint; applied by the dashboard
 * on sign-in (palette + typography + brand assets). Distinct from the tenant's
 * self-service appearance (`identity/appearance`).
 */
export type TenantPalette = {
  primary: string;
  secondary: string;
  tertiary: string;
  background: string;
  surface: string;
  error: string;
  warning: string;
  success: string;
  info: string;
};

export type TenantBrandAssets = {
  logoUrl?: string | null;
  logoDarkUrl?: string | null;
  faviconUrl?: string | null;
};

export type TenantTypography = {
  fontFamily: string;
  headingFontFamily: string;
  fontSizeBase: number;
  lineHeightBase: number;
};

export type TenantBrandingDto = {
  lightPalette: TenantPalette;
  darkPalette: TenantPalette;
  brandAssets: TenantBrandAssets;
  typography: TenantTypography;
  isDefault: boolean;
};

/** Current tenant's operator-configured branding (self-service read). */
export function getCurrentTenantBranding(): Promise<TenantBrandingDto> {
  return apiFetch<TenantBrandingDto>("/api/v1/tenants/theme/current");
}
