/**
 * LocalizationContext — tenant-wide date/time/currency/number formatting.
 *
 * Architecture:
 *  - On mount: reads from localStorage ("maka-locale-v1") for instant paint.
 *  - Fetches from GET /api/v1/identity/localization via TanStack Query.
 *  - When config.language changes: calls i18next.changeLanguage().
 *  - Exposes formatDateTime / formatCurrency / formatNumber helpers that all
 *    consume the live config; formatters never need the config directly.
 *
 * Only one row exists per tenant (UserId = null). The API seeds Bogotá/COP/es
 * defaults on first call so the provider always has a valid config.
 */

import i18next from "i18next";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  type ReactNode,
} from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/auth/use-auth";
import {
  getTenantLocalization,
  updateTenantLocalization,
  type LocalizationConfig,
} from "@/api/identity";

// ─── Types ───────────────────────────────────────────────────────────────────

export type { LocalizationConfig };

export type LocalizationContextValue = {
  config: LocalizationConfig;
  isLoading: boolean;
  /** Persist a full config update. Resolves once the server confirms. */
  update: (next: LocalizationConfig) => Promise<void>;
  /** Format an ISO date-time string to a locale-aware short string. */
  formatDateTime: (iso: string) => string;
  /** Format a date-only ISO string (or Date). */
  formatDate: (iso: string | Date) => string;
  /** Format an amount as currency per the tenant config. */
  formatCurrency: (amount: number) => string;
  /** Format a plain number with the tenant's grouping/decimal style. */
  formatNumber: (value: number, decimals?: number) => string;
};

// ─── Defaults ────────────────────────────────────────────────────────────────

const DEFAULT_CONFIG: LocalizationConfig = {
  timezone: "America/Bogota",
  dateFormat: "DD/MM/YYYY",
  timeFormat: "12h",
  currency: "COP",
  language: "es",
  numberFormat: "1.000,00",
};

const STORAGE_KEY = "maka-locale-v1";

function readCache(): LocalizationConfig | null {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as LocalizationConfig;
  } catch {
    return null;
  }
}

function writeCache(config: LocalizationConfig) {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
  } catch {
    /* storage unavailable */
  }
}

// ─── Formatting helpers ───────────────────────────────────────────────────────

/** Derive a Intl.DateTimeFormat locale from the config's language tag. */
function intlLocale(config: LocalizationConfig): string {
  if (config.language === "es") return "es-CO";
  return "en-US";
}

/** Derive Intl.DateTimeFormat options for a short date. */
function dateOptions(config: LocalizationConfig): Intl.DateTimeFormatOptions {
  // Respect the config dateFormat token.
  switch (config.dateFormat) {
    case "MM/DD/YYYY":
      return { month: "2-digit", day: "2-digit", year: "numeric" };
    case "YYYY-MM-DD":
      return { year: "numeric", month: "2-digit", day: "2-digit" };
    default: // "DD/MM/YYYY"
      return { day: "2-digit", month: "2-digit", year: "numeric" };
  }
}

function buildDateTimeFormatter(config: LocalizationConfig): Intl.DateTimeFormat {
  return new Intl.DateTimeFormat(intlLocale(config), {
    timeZone: config.timezone,
    ...dateOptions(config),
    hour: "2-digit",
    minute: "2-digit",
    hour12: config.timeFormat === "12h",
  });
}

function buildDateFormatter(config: LocalizationConfig): Intl.DateTimeFormat {
  return new Intl.DateTimeFormat(intlLocale(config), {
    timeZone: config.timezone,
    ...dateOptions(config),
  });
}

function buildCurrencyFormatter(config: LocalizationConfig): Intl.NumberFormat {
  return new Intl.NumberFormat(intlLocale(config), {
    style: "currency",
    currency: config.currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  });
}

function buildNumberFormatter(
  config: LocalizationConfig,
  decimals: number,
): Intl.NumberFormat {
  return new Intl.NumberFormat(intlLocale(config), {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}

// ─── Context ─────────────────────────────────────────────────────────────────

const LocalizationContext = createContext<LocalizationContextValue | null>(null);

// ─── Provider ────────────────────────────────────────────────────────────────

export function LocalizationProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const queryClient = useQueryClient();

  const { data: serverConfig, isLoading } = useQuery<LocalizationConfig>({
    queryKey: ["identity", "localization"],
    queryFn: getTenantLocalization,
    // Use cached version as placeholder for instant paint
    placeholderData: () => readCache() ?? DEFAULT_CONFIG,
    // Re-fetch every 15 min (config is rarely changed)
    staleTime: 15 * 60 * 1000,
    enabled: isAuthenticated,
  });

  const config = serverConfig ?? readCache() ?? DEFAULT_CONFIG;

  // Persist to localStorage whenever the server config arrives or changes.
  useEffect(() => {
    if (serverConfig) writeCache(serverConfig);
  }, [serverConfig]);

  // Sync i18next language when config.language changes.
  useEffect(() => {
    if (config.language && i18next.language !== config.language) {
      void i18next.changeLanguage(config.language);
    }
  }, [config.language]);

  const { mutateAsync } = useMutation<LocalizationConfig, Error, LocalizationConfig>({
    mutationFn: updateTenantLocalization,
    onSuccess: (updated) => {
      queryClient.setQueryData(["identity", "localization"], updated);
      writeCache(updated);
    },
  });

  const update = useCallback(
    async (next: LocalizationConfig) => {
      await mutateAsync(next);
    },
    [mutateAsync],
  );

  const formatDateTime = useCallback(
    (iso: string): string => {
      try {
        return buildDateTimeFormatter(config).format(new Date(iso));
      } catch {
        return iso;
      }
    },
    [config],
  );

  const formatDate = useCallback(
    (iso: string | Date): string => {
      try {
        return buildDateFormatter(config).format(
          iso instanceof Date ? iso : new Date(iso),
        );
      } catch {
        return String(iso);
      }
    },
    [config],
  );

  const formatCurrency = useCallback(
    (amount: number): string => {
      try {
        return buildCurrencyFormatter(config).format(amount);
      } catch {
        return String(amount);
      }
    },
    [config],
  );

  const formatNumber = useCallback(
    (value: number, decimals = 2): string => {
      try {
        return buildNumberFormatter(config, decimals).format(value);
      } catch {
        return String(value);
      }
    },
    [config],
  );

  const value = useMemo<LocalizationContextValue>(
    () => ({
      config,
      isLoading,
      update,
      formatDateTime,
      formatDate,
      formatCurrency,
      formatNumber,
    }),
    [config, isLoading, update, formatDateTime, formatDate, formatCurrency, formatNumber],
  );

  return (
    <LocalizationContext.Provider value={value}>
      {children}
    </LocalizationContext.Provider>
  );
}

// ─── Hook ────────────────────────────────────────────────────────────────────

export function useLocalization(): LocalizationContextValue {
  const ctx = useContext(LocalizationContext);
  if (!ctx) throw new Error("useLocalization must be used within LocalizationProvider");
  return ctx;
}
